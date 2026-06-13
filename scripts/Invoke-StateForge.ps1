<#
.SYNOPSIS
Runs StateForge convenience commands.

.DESCRIPTION
Provides a small convenience command runner for operations that do not require
rich parameter binding. Parameter-heavy operational scripts remain dedicated
PowerShell entry points so their parameters, prompts, help, and validation stay visible.

Use Test-StateForge.ps1 for validation suites.

.PARAMETER Command
Convenience command to run.

.PARAMETER Arguments
Optional hashtable forwarded to the underlying focused script when appropriate.

.EXAMPLE
.\scripts\Invoke-StateForge.ps1 -Command BuildPackages

.EXAMPLE
.\scripts\Invoke-StateForge.ps1 -Command RunSmokeTest

.EXAMPLE
.\scripts\Invoke-StateForge.ps1 -Command TestNuGetSources

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
    [ValidateSet(
        'BuildPackages',
        'CompareBenchmark',
        'RunBenchmark',
        'RunCleanup',
        'RunFarmTest',
        'RunResilienceTest',
        'RunScaleTest',
        'RunSmokeTest',
        'RepairSolution',
        'ShowSmokeDemo',
        'TestNuGetSources'
    )]
    [string]$Command,

    [Parameter()]
    [hashtable]$Arguments
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Invoke-StateForgeCommandScript {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required script not found: $Path"
    }

    Write-Host "==> $Path"

    if ($null -ne $Arguments -and $Arguments.Count -gt 0) {
        & $Path @Arguments
    }
    else {
        & $Path
    }

    if (-not $?) {
        throw "$Path failed."
    }
}

try {
    switch ($Command) {
        'BuildPackages' { Invoke-StateForgeCommandScript -Path '.\scripts\Build-StateForgePackages.ps1' }
        'CompareBenchmark' { Invoke-StateForgeCommandScript -Path '.\scripts\Compare-StateForgeBenchmark.ps1' }
        'RunBenchmark' { Invoke-StateForgeCommandScript -Path '.\scripts\Invoke-StateForgeBenchmark.ps1' }
        'RunCleanup' { Invoke-StateForgeCommandScript -Path '.\scripts\Invoke-StateForgeCleanup.ps1' }
        'RunFarmTest' { Invoke-StateForgeCommandScript -Path '.\scripts\Invoke-StateForgeFarmTest.ps1' }
        'RunResilienceTest' { Invoke-StateForgeCommandScript -Path '.\scripts\Invoke-StateForgeResilienceTest.ps1' }
        'RunScaleTest' { Invoke-StateForgeCommandScript -Path '.\scripts\Invoke-StateForgeScaleTest.ps1' }
        'RunSmokeTest' { Invoke-StateForgeCommandScript -Path '.\scripts\Invoke-StateForgeSmokeTest.ps1' }
        'RepairSolution' { Invoke-StateForgeCommandScript -Path '.\scripts\Repair-StateForgeSolution.ps1' }
        'ShowSmokeDemo' { Invoke-StateForgeCommandScript -Path '.\scripts\Show-StateForgeSmokeDemo.ps1' }
        'TestNuGetSources' { Invoke-StateForgeCommandScript -Path '.\scripts\Test-NuGetSources.ps1' }
    }

    [PSCustomObject]@{
        Command = $Command
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
