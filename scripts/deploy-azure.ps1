[CmdletBinding()]
param(
    [string]$EnvironmentName = 'carddemo-xm-dev',
    [string]$Location = 'australiaeast',
    [string]$SubscriptionId = (az account show --query id -o tsv)
)

$ErrorActionPreference = 'Stop'
if ($EnvironmentName -notmatch '^[a-z0-9](?:[a-z0-9-]{0,20}[a-z0-9])$') {
    throw 'EnvironmentName must be 2-22 lowercase alphanumeric or hyphen characters and cannot start or end with a hyphen.'
}
if ($Location -notmatch '^[a-z][a-z0-9]{2,29}$') {
    throw 'Location must be a lowercase Azure region short name, for example australiaeast.'
}
if ($SubscriptionId -notmatch '^[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}$') {
    throw 'SubscriptionId must be a GUID.'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$resourceGroup = "rg-$EnvironmentName"
$timestamp = [DateTime]::UtcNow.ToString('yyyyMMddHHmmss')
$deploymentName = "carddemo-$timestamp"
$revisionSuffix = "r$([DateTime]::UtcNow.ToString('MMddHHmmss'))"
$shortCommit = git -C $repoRoot rev-parse --short HEAD 2>$null
$workingTreeChanges = git -C $repoRoot status --porcelain 2>$null
$imageTag = if ([string]::IsNullOrWhiteSpace($shortCommit) -or $workingTreeChanges) {
    "dev-$timestamp"
}
else {
    "$shortCommit-$timestamp"
}

function Invoke-InfrastructureDeployment {
    param(
        [Parameter(Mandatory)]
        [string]$Name,
        [Parameter(Mandatory)]
        [bool]$ExistingWeb,
        [Parameter(Mandatory)]
        [bool]$ExternalIngress
    )

    $parameters = @(
        "environmentName=$EnvironmentName",
        "location=$Location",
        "webExists=$($ExistingWeb.ToString().ToLowerInvariant())",
        "externalIngress=$($ExternalIngress.ToString().ToLowerInvariant())"
    )
    $outputJson = az deployment sub create `
        --name $Name `
        --location $Location `
        --template-file .\infra\main.bicep `
        --parameters $parameters `
        --query properties.outputs `
        --output json
    if ($LASTEXITCODE -ne 0) { throw "Azure infrastructure deployment '$Name' failed." }

    return $outputJson | ConvertFrom-Json
}

Push-Location $repoRoot
try {
    az account set --subscription $SubscriptionId
    if ($LASTEXITCODE -ne 0) { throw 'Unable to select the Azure subscription.' }

    foreach ($provider in 'Microsoft.App', 'Microsoft.ContainerRegistry', 'Microsoft.OperationalInsights') {
        az provider register --namespace $provider --wait
        if ($LASTEXITCODE -ne 0) { throw "Unable to register $provider." }
    }

    az extension add --name containerapp --upgrade --allow-preview true --only-show-errors
    if ($LASTEXITCODE -ne 0) { throw 'Unable to install the Azure Container Apps CLI extension.' }

    $groupExists = az group exists --name $resourceGroup | ConvertFrom-Json
    $existingApps = @()
    if ($groupExists) {
        $existingAppsJson = az containerapp list `
            --resource-group $resourceGroup `
            --output json
        if ($LASTEXITCODE -ne 0) { throw 'Unable to inspect existing Container Apps.' }
        $parsedApps = @($existingAppsJson | ConvertFrom-Json)
        $existingApps = @($parsedApps | Where-Object {
            $_.tags.'azd-service-name' -eq 'web'
        } | Select-Object -ExpandProperty name)
    }
    if ($existingApps.Count -gt 1) {
        throw "Resource group $resourceGroup contains multiple Container Apps tagged as the web service."
    }

    $webExists = $existingApps.Count -eq 1
    $externalOnFirstPass = $webExists
    $whatIfParameters = @(
        "environmentName=$EnvironmentName",
        "location=$Location",
        "webExists=$($webExists.ToString().ToLowerInvariant())",
        "externalIngress=$($externalOnFirstPass.ToString().ToLowerInvariant())"
    )

    az deployment sub what-if `
        --name "$deploymentName-whatif" `
        --location $Location `
        --template-file .\infra\main.bicep `
        --parameters $whatIfParameters
    if ($LASTEXITCODE -ne 0) { throw 'Azure deployment what-if failed.' }

    $outputs = Invoke-InfrastructureDeployment `
        -Name $deploymentName `
        -ExistingWeb $webExists `
        -ExternalIngress $externalOnFirstPass
    $registryName = $outputs.AZURE_CONTAINER_REGISTRY_NAME.value
    $registryEndpoint = $outputs.AZURE_CONTAINER_REGISTRY_ENDPOINT.value
    $webName = $outputs.SERVICE_WEB_NAME.value

    az acr build `
        --registry $registryName `
        --image "carddemo-web:$imageTag" `
        --platform linux/amd64 `
        .
    if ($LASTEXITCODE -ne 0) { throw 'Azure Container Registry build failed.' }

    $digest = az acr repository show `
        --name $registryName `
        --image "carddemo-web:$imageTag" `
        --query digest `
        --output tsv
    if ($LASTEXITCODE -ne 0 -or $digest -notmatch '^sha256:[a-f0-9]{64}$') {
        throw 'Unable to resolve the immutable ACR image digest.'
    }
    $image = "$registryEndpoint/carddemo-web@$digest"

    az containerapp update `
        --name $webName `
        --resource-group $resourceGroup `
        --image $image `
        --revision-suffix $revisionSuffix `
        --output none
    if ($LASTEXITCODE -ne 0) { throw 'Container App image update failed.' }

    $expectedRevision = "$webName--$revisionSuffix"
    $revisionReady = $false
    for ($attempt = 1; $attempt -le 60; $attempt++) {
        $revisionJson = az containerapp revision show `
            --name $webName `
            --resource-group $resourceGroup `
            --revision $expectedRevision `
            --query '{health:properties.healthState,running:properties.runningState,image:properties.template.containers[0].image}' `
            --output json 2>$null
        if ($LASTEXITCODE -eq 0) {
            $revision = $revisionJson | ConvertFrom-Json
            if ($revision.health -eq 'Healthy' -and
                $revision.running -eq 'Running' -and
                $revision.image -eq $image) {
                $revisionReady = $true
                break
            }
        }

        Start-Sleep -Seconds 5
    }
    if (-not $revisionReady) {
        throw "Revision $expectedRevision did not become healthy with the expected image digest."
    }

    if (-not $webExists) {
        $outputs = Invoke-InfrastructureDeployment `
            -Name "$deploymentName-expose" `
            -ExistingWeb $true `
            -ExternalIngress $true
    }

    $serviceUrl = $outputs.SERVICE_WEB_URI.value
    & "$PSScriptRoot\smoke-test.ps1" -BaseUrl $serviceUrl

    [pscustomobject]@{
        ServiceUrl = $serviceUrl
        Revision = $expectedRevision
        Image = $image
    } | Format-List
}
finally {
    Pop-Location
}
