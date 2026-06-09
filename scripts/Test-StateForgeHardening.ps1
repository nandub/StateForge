<#
.SYNOPSIS
Runs StateForge v0.26.2 hardening checks.

.DESCRIPTION
Runs layout validation, source validation, snapshot marker validation, recovery-flow validation, and package metadata validation when available.

.EXAMPLE
.\scripts\Test-StateForgeHardening.ps1

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
    .\scripts\Test-StateForgeLayout.ps1 | Out-Host
    .\scripts\Test-StateForgeSource.ps1 | Out-Host
    .\scripts\Test-StateForgeSnapshotMarkers.ps1 | Out-Host
    .\scripts\Test-StateForgeRecoveryFlow.ps1 | Out-Host

    if (Test-Path -LiteralPath '.\scripts\Test-StateForgePackageMetadata.ps1') {
        .\scripts\Test-StateForgePackageMetadata.ps1 | Out-Host
    }

    [PSCustomObject]@{
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
