<#
.SYNOPSIS
Runs a larger StateForge scale validation.

.DESCRIPTION
Runs a larger scale test intended for manual pre-release validation.

.PARAMETER Sessions
Number of sessions.

.EXAMPLE
.\scripts\Test-StateForgeLargeScale.ps1 -Sessions 100000

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
    [int]$Sessions = 100000
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    .\scripts\Invoke-StateForgeScaleTest.ps1 -Sessions $Sessions -PayloadBytes 1024 -Threads 8

    [PSCustomObject]@{
        Sessions = $Sessions
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
