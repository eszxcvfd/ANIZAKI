param(
    [switch]$SkipRestore
)

$ErrorActionPreference = "Stop"

function Invoke-Step {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "==> $Name" -ForegroundColor Cyan
    & $Command
}

Push-Location (Resolve-Path (Join-Path $PSScriptRoot ".."))
try {
    if (-not $SkipRestore) {
        Invoke-Step -Name "Backend restore" -Command { dotnet restore src/api/Anizaki.Api.sln }
    }

    Invoke-Step -Name "Backend build" -Command { dotnet build src/api/Anizaki.Api.sln }
    Invoke-Step -Name "Backend tests" -Command { dotnet test src/api/Anizaki.Api.sln }
    Invoke-Step -Name "Frontend lint" -Command { pnpm.cmd --dir src/web lint }
    Invoke-Step -Name "Frontend tests" -Command { pnpm.cmd --dir src/web test }
    Invoke-Step -Name "Frontend build" -Command { pnpm.cmd --dir src/web build }

    Write-Host ""
    Write-Host "Verification matrix completed successfully." -ForegroundColor Green
}
finally {
    Pop-Location
}
