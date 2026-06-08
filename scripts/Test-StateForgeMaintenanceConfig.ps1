<#
.SYNOPSIS
Validates a StateForge Maintenance Host configuration file.

.DESCRIPTION
Runs the Maintenance Host configuration validation mode.

.PARAMETER Config
Path to the JSON configuration file.

.EXAMPLE
.\scripts\Test-StateForgeMaintenanceConfig.ps1 -Config .\config\stateforge-maintenance.sample.json

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Config
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    .\scripts\Invoke-StateForgeMaintenanceHost.ps1 -Config $Config -ValidateConfig

    [PSCustomObject]@{
        Config  = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Config)
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
