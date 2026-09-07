[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [uri]$BaseUrl
)

$ErrorActionPreference = 'Stop'
if (-not $BaseUrl.IsAbsoluteUri -or
    -not ($BaseUrl.Scheme -eq 'https' -or ($BaseUrl.Scheme -eq 'http' -and $BaseUrl.IsLoopback)) -or
    $BaseUrl.UserInfo -or $BaseUrl.Query -or $BaseUrl.Fragment -or $BaseUrl.AbsolutePath -ne '/') {
    throw 'BaseUrl must be an HTTPS origin or loopback HTTP origin, without credentials.'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$previousTarget = [Environment]::GetEnvironmentVariable('CARDDEMO_TEST_BASE_URL', 'Process')
Push-Location $repoRoot
try {
    & "$PSScriptRoot\smoke-test.ps1" -BaseUrl $BaseUrl.AbsoluteUri
    dotnet restore .\CardDemo.Azure.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Locked restore failed.' }

    $env:CARDDEMO_TEST_BASE_URL = $BaseUrl.AbsoluteUri
    dotnet test .\CardDemo.Azure.slnx --configuration Release --no-restore `
        --filter 'FullyQualifiedName~CardDemo.Modern.Tests.AppRoutesTests'
    if ($LASTEXITCODE -ne 0) { throw 'Read-only HTTP contract failed.' }
}
finally {
    [Environment]::SetEnvironmentVariable('CARDDEMO_TEST_BASE_URL', $previousTarget, 'Process')
    Pop-Location
}
