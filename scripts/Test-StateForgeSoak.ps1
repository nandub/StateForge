<#
.SYNOPSIS
Runs the short StateForge soak validation gate.

.DESCRIPTION
Executes a bounded soak workload that validates the soak harness, report generation,
cleanup, replication, snapshot creation, and final data verification without making
Production validation long-running.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    $outputPath = Join-Path -Path $repoRoot -ChildPath 'artifacts\soak-validation'

    & (Join-Path -Path $PSScriptRoot -ChildPath 'Invoke-StateForgeSoakTest.ps1') `
        -Sessions 64 `
        -PayloadBytes 512 `
        -Threads 2 `
        -DurationSeconds 5 `
        -MaxOperations 300 `
        -CleanupInterval 50 `
        -ReplicationInterval 0 `
        -SnapshotInterval 0 `
        -FinalReplication `
        -FinalSnapshot `
        -OutputPath $outputPath | Out-Host

    $jsonPath = Join-Path -Path $outputPath -ChildPath 'soak.json'
    $csvPath = Join-Path -Path $outputPath -ChildPath 'soak.csv'
    $jsonText = Get-Content -LiteralPath $jsonPath -Raw
    $csvText = Get-Content -LiteralPath $csvPath -Raw

    foreach ($requiredPattern in @(
        '"mode": "soak"',
        '"errorCount": 0',
        '"name": "create/update"',
        '"name": "read"',
        '"name": "refresh"',
        '"name": "lock-update"',
        '"name": "cleanup"',
        '"name": "replication"',
        '"name": "snapshot"'
    )) {
        if ($jsonText -notmatch [regex]::Escape($requiredPattern)) {
            throw "Soak JSON report is missing required content: $requiredPattern"
        }
    }

    if ($csvText -notmatch 'create/update' -or $csvText -notmatch 'snapshot') {
        throw 'Soak CSV report is missing required scenario rows.'
    }

    [PSCustomObject]@{
        JsonPath = $jsonPath
        CsvPath  = $csvPath
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
