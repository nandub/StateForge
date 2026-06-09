<#
.SYNOPSIS
Runs a fast StateForge scale validation.

.DESCRIPTION
Runs a smaller scale test suitable for CI or local validation.

.EXAMPLE
.\scripts\Test-StateForgeScale.ps1

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
    .\scripts\Invoke-StateForgeScaleTest.ps1 -Sessions 2000 -PayloadBytes 512 -Threads 4 -ExportJson .\artifacts\benchmarks\scale-fast.json -ExportCsv .\artifacts\benchmarks\scale-fast.csv

    [PSCustomObject]@{
        Sessions = 2000
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
