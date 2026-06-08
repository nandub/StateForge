[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $root = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath 'StateForgeMaintenanceHostTest'

    if (Test-Path -LiteralPath $root) {
        Remove-Item -LiteralPath $root -Recurse -Force
    }

    New-Item -Path $root -ItemType Directory -Force | Out-Null

    .\scripts\Invoke-StateForgeMaintenanceHost.ps1 -RootPath $root -Once -Json
    .\scripts\Invoke-StateForgeMaintenanceHost.ps1 -RootPath $root -Once -HealthOnly -Json
    .\scripts\Invoke-StateForgeMaintenanceHost.ps1 -RootPath $root -Once -StatsOnly -Json
    .\scripts\Invoke-StateForgeMaintenanceHost.ps1 -RootPath $root -Once -CleanupOnly -Json

    [PSCustomObject]@{
        RootPath = $root
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
