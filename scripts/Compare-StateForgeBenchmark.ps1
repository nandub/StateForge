<#
.SYNOPSIS
Compares two StateForge benchmark CSV files.

.DESCRIPTION
Loads two benchmark CSV exports and compares elapsed time and operations-per-second by scenario.

.PARAMETER BaselineCsv
Baseline benchmark CSV.

.PARAMETER CandidateCsv
Candidate benchmark CSV.

.PARAMETER MinimumThroughputPercent
Minimum candidate throughput as a percentage of the baseline. Defaults to 15.

.PARAMETER MaximumLatencyMultiplier
Maximum candidate P95 latency relative to the baseline. Defaults to 8.

.PARAMETER LatencyAllowanceMs
Fixed P95 allowance for short operations. Defaults to 25 milliseconds.

.EXAMPLE
.\scripts\Compare-StateForgeBenchmark.ps1 -BaselineCsv .\old.csv -CandidateCsv .\new.csv

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
    [string]$BaselineCsv,

    [Parameter(Mandatory = $true)]
    [string]$CandidateCsv,

    [Parameter()]
    [ValidateRange(1, 100)]
    [double]$MinimumThroughputPercent = 15,

    [Parameter()]
    [ValidateRange(1, 100)]
    [double]$MaximumLatencyMultiplier = 8,

    [Parameter()]
    [ValidateRange(0, 60000)]
    [double]$LatencyAllowanceMs = 25
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $baselinePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($BaselineCsv)
    $candidatePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($CandidateCsv)

    $baseline = Import-Csv -LiteralPath $baselinePath
    $candidate = Import-Csv -LiteralPath $candidatePath
    $comparisons = @()

    foreach ($baseRow in $baseline) {
        $match = $candidate | Where-Object { $_.name -eq $baseRow.name } | Select-Object -First 1

        if ($null -eq $match) {
            throw "Candidate benchmark is missing scenario: $($baseRow.name)"
        }

        $baseOps = [double]$baseRow.opsPerSecond
        $candidateOps = [double]$match.opsPerSecond
        $baseP95 = [double]$baseRow.p95Ms
        $candidateP95 = [double]$match.p95Ms
        $deltaPercent = 0

        if ($baseOps -gt 0) {
            $deltaPercent = (($candidateOps - $baseOps) / $baseOps) * 100
        }

        $minimumOps = $baseOps * ($MinimumThroughputPercent / 100)
        $maximumP95 = ($baseP95 * $MaximumLatencyMultiplier) + $LatencyAllowanceMs
        $success = $candidateOps -ge $minimumOps -and $candidateP95 -le $maximumP95

        $comparisons += [PSCustomObject]@{
            Scenario            = $baseRow.name
            BaselineOpsPerSec   = $baseOps
            CandidateOpsPerSec  = $candidateOps
            DeltaPercent        = [Math]::Round($deltaPercent, 2)
            BaselineP95Ms       = $baseP95
            CandidateP95Ms      = $candidateP95
            MinimumOpsPerSec    = [Math]::Round($minimumOps, 3)
            MaximumP95Ms        = [Math]::Round($maximumP95, 3)
            Success             = $success
        }
    }

    $comparisons | Format-Table -AutoSize | Out-Host
    $failures = @($comparisons | Where-Object { -not $_.Success })
    if ($failures.Count -gt 0) {
        throw "Performance regression threshold exceeded for: $($failures.Scenario -join ', ')"
    }

    $comparisons
}
catch {
    Write-Error -ErrorRecord $_
}
