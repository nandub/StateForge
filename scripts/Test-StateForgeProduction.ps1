<#
.SYNOPSIS
Runs StateForge production-readiness validation.

.DESCRIPTION
Runs the production-readiness validation suite through the consolidated StateForge test runner.

.EXAMPLE
.\scripts\Test-StateForgeProduction.ps1

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
    .\scripts\Test-StateForge.ps1 -Suite Production
}
catch {
    Write-Error -ErrorRecord $_
}
