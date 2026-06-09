<#
.SYNOPSIS
Compares two StateForge benchmark CSV files.

.DESCRIPTION
Loads two benchmark CSV exports and compares elapsed time and operations-per-second by scenario.

.PARAMETER BaselineCsv
Baseline benchmark CSV.

.PARAMETER CandidateCsv
Candidate benchmark CSV.

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
    [string]$CandidateCsv
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $baselinePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($BaselineCsv)
    $candidatePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($CandidateCsv)

    $baseline = Import-Csv -LiteralPath $baselinePath
    $candidate = Import-Csv -LiteralPath $candidatePath

    foreach ($baseRow in $baseline) {
        $match = $candidate | Where-Object { $_.name -eq $baseRow.name } | Select-Object -First 1

        if ($null -ne $match) {
            $baseOps = [double]$baseRow.opsPerSecond
            $candidateOps = [double]$match.opsPerSecond
            $deltaPercent = 0

            if ($baseOps -gt 0) {
                $deltaPercent = (($candidateOps - $baseOps) / $baseOps) * 100
            }

            [PSCustomObject]@{
                Scenario            = $baseRow.name
                BaselineOpsPerSec   = $baseOps
                CandidateOpsPerSec  = $candidateOps
                DeltaPercent        = [Math]::Round($deltaPercent, 2)
                BaselineP95Ms       = [double]$baseRow.p95Ms
                CandidateP95Ms      = [double]$match.p95Ms
            }
        }
    }
}
catch {
    Write-Error -ErrorRecord $_
}
