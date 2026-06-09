[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    .\scripts\Test-StateForgePerformance.ps1

    [PSCustomObject]@{
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
