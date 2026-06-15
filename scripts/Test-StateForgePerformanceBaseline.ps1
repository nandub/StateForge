<#
.SYNOPSIS
Validates StateForge against the reviewed small performance baseline.

.DESCRIPTION
Runs the small workload into artifacts and compares it with the tracked baseline using broad,
relative thresholds intended to catch substantial regressions without asserting machine-specific timing.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    $baselinePath = Join-Path -Path $repoRoot -ChildPath 'performance-baselines\small.csv'
    $candidateRoot = Join-Path -Path $repoRoot -ChildPath 'artifacts\performance-validation'
    $candidatePath = Join-Path -Path $candidateRoot -ChildPath 'small.csv'

    if (-not (Test-Path -LiteralPath $baselinePath)) {
        throw "Missing tracked performance baseline: $baselinePath"
    }

    & (Join-Path -Path $PSScriptRoot -ChildPath 'Invoke-StateForgePerformanceBaseline.ps1') `
        -Profile Small `
        -OutputPath $candidateRoot | Out-Host

    $comparison = @(
        & (Join-Path -Path $PSScriptRoot -ChildPath 'Compare-StateForgeBenchmark.ps1') `
            -BaselineCsv $baselinePath `
            -CandidateCsv $candidatePath
    )

    [PSCustomObject]@{
        ScenarioCount = $comparison.Count
        BaselinePath  = $baselinePath
        CandidatePath = $candidatePath
        Success       = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
