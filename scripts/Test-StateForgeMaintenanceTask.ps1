<#
.SYNOPSIS
Validates Scheduled Task helper wiring using -WhatIf.

.DESCRIPTION
Runs the scheduled task registration and unregister scripts in -WhatIf mode.

.PARAMETER RootPath
StateForge root path.

.EXAMPLE
.\scripts\Test-StateForgeMaintenanceTask.ps1 -RootPath ..\StateForgeSmoke\demo

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$RootPath = '.'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    .\scripts\Register-StateForgeMaintenanceTask.ps1 -RootPath $RootPath -FrequencyMinutes 15 -WhatIf | Out-Null
    .\scripts\Unregister-StateForgeMaintenanceTask.ps1 -WhatIf | Out-Null

    [PSCustomObject]@{
        RootPath = $RootPath
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
