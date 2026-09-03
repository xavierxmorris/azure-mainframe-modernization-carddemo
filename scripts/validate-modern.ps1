[CmdletBinding()]
param(
    [switch]$SkipContainer,
    [string]$UpstreamCommit = '59cc6c2fd7ebd7ef7925cad552a01a4b8b6e4d5e'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot

Push-Location $repoRoot
try {
    git cat-file -e "$UpstreamCommit^{commit}"
    if ($LASTEXITCODE -ne 0) { throw 'The expected upstream commit is unavailable.' }

    git diff --exit-code --diff-filter=MDRT $UpstreamCommit -- app samples diagrams scripts
    if ($LASTEXITCODE -ne 0) {
        throw 'A preserved upstream mainframe asset was modified, deleted, renamed, or type-changed.'
    }

    dotnet restore .\CardDemo.Azure.slnx --locked-mode
    if ($LASTEXITCODE -ne 0) { throw 'Locked dotnet restore failed.' }

    dotnet format .\CardDemo.Azure.slnx --verify-no-changes --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet format verification failed.' }

    dotnet test .\CardDemo.Azure.slnx --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed.' }

    az bicep build --file .\infra\main.bicep
    if ($LASTEXITCODE -ne 0) { throw 'Bicep validation failed.' }

    if (-not $SkipContainer) {
        $image = 'carddemo-azure:local'
        docker build --tag $image .
        if ($LASTEXITCODE -ne 0) { throw 'Container build failed.' }

        $listener = [System.Net.Sockets.TcpListener]::new(
            [System.Net.IPAddress]::Loopback,
            0)
        $listener.Start()
        $port = ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
        $listener.Stop()

        $containerId = docker run --rm --detach --publish "127.0.0.1:${port}:8080" $image
        if ($LASTEXITCODE -ne 0) { throw 'Container start failed.' }

        try {
            & "$PSScriptRoot\smoke-test.ps1" `
                -BaseUrl "http://127.0.0.1:$port" `
                -Attempts 30 `
                -DelaySeconds 1

            $containerUser = docker inspect $containerId --format '{{.Config.User}}'
            $platform = docker image inspect $image --format '{{.Os}}/{{.Architecture}}'
            if ([string]::IsNullOrWhiteSpace($containerUser) -or
                $containerUser -in @('0', 'root')) {
                throw 'The production container is not configured with a non-root user.'
            }
            if ($platform -ne 'linux/amd64') {
                throw "Unexpected container platform: $platform."
            }
        }
        finally {
            docker stop $containerId | Out-Null
        }
    }
}
finally {
    Pop-Location
}
