<#
.SYNOPSIS
Validates that StateForge production-readiness validation is non-interactive.

.DESCRIPTION
Checks that Test-StateForge.ps1 provides RootPath to Test-StateForgeHealth.ps1
when running the Production suite.

.EXAMPLE
.\scripts\Test-StateForgeProductionNonInteractive.ps1

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
    $runnerPath = '.\scripts\Test-StateForge.ps1'

    if (-not (Test-Path -LiteralPath $runnerPath)) {
        throw "Missing test runner: $runnerPath"
    }

    $runnerText = Get-Content -LiteralPath $runnerPath -Raw

    if ($runnerText -notmatch 'Get-StateForgeProductionHealthRoot') {
        throw 'Production suite must define a default health root.'
    }

    if ($runnerText -notmatch "Test-StateForgeHealth\.ps1'\s+-Arguments\s+@\{\s*RootPath") {
        throw 'Production suite must pass RootPath to Test-StateForgeHealth.ps1.'
    }

    [PSCustomObject]@{
        RunnerPath     = (Resolve-Path -LiteralPath $runnerPath).Path
        NonInteractive = $true
        Success        = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
