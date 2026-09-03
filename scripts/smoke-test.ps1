[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$BaseUrl,
    [int]$Attempts = 24,
    [int]$DelaySeconds = 5
)

$ErrorActionPreference = 'Stop'
$base = $BaseUrl.TrimEnd('/')

function Invoke-CardDemoRequest {
    param(
        [Parameter(Mandatory)]
        [scriptblock]$Request
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            return & $Request
        }
        catch {
            if ($attempt -eq $Attempts) {
                throw
            }

            Write-Host "Request attempt $attempt failed; retrying in $DelaySeconds seconds."
            Start-Sleep -Seconds $DelaySeconds
        }
    }
}

$health = Invoke-CardDemoRequest {
    Invoke-WebRequest -Uri "$base/health" -UseBasicParsing -TimeoutSec 20
}
if ($health.StatusCode -ne 200) {
    throw "Health endpoint returned $($health.StatusCode)."
}

$summary = Invoke-CardDemoRequest {
    Invoke-RestMethod -Uri "$base/api/summary" -TimeoutSec 20
}
if ($summary.accountCount -ne 50 -or $summary.transactionCount -ne 300) {
    throw "Unexpected summary counts: $($summary | ConvertTo-Json -Compress)."
}

$account = Invoke-CardDemoRequest {
    Invoke-RestMethod -Uri "$base/api/accounts/00000000001" -TimeoutSec 20
}
$maskedNumber = [string]$account.cards[0].maskedNumber
if ($account.cards.Count -lt 1 -or $maskedNumber -notmatch '\d{4}$' -or $maskedNumber -match '^\d{16}$') {
    throw 'Account API did not return the expected masked card view.'
}

Write-Host "Smoke tests passed for $base" -ForegroundColor Green
