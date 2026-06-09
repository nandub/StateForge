<#
.SYNOPSIS
Runs StateForge observability validation.

.DESCRIPTION
Runs Dashboard and Prometheus observability tests.

.EXAMPLE
.\scripts\Test-StateForgeObservability.ps1

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    .\scripts\Test-StateForgeDashboard.ps1
    .\scripts\Test-StateForgePrometheus.ps1

    [PSCustomObject]@{
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
