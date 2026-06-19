<#
.SYNOPSIS
Runs reviewed StateForge performance baseline profiles.

.DESCRIPTION
Runs small, medium, or large workloads. Candidate output is written under artifacts by default.
Use UpdateBaseline only after reviewing the machine and workload results; reviewed baselines are
stored under performance-baselines so they remain available from a clean clone.

.PARAMETER Profile
Profile to run: Small, Medium, Large, or All.

.PARAMETER OutputPath
Candidate output directory.

.PARAMETER UpdateBaseline
Writes results to the tracked performance-baselines directory.

.EXAMPLE
.\scripts\Invoke-StateForgePerformanceBaseline.ps1 -Profile All

.EXAMPLE
.\scripts\Invoke-StateForgePerformanceBaseline.ps1 -Profile All -UpdateBaseline

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Small', 'Medium', 'Large', 'All')]
    [string]$Profile = 'All',

    [Parameter()]
    [string]$OutputPath = '.\artifacts\performance',

    [Parameter()]
    [switch]$UpdateBaseline
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    . (Join-Path -Path $PSScriptRoot -ChildPath 'StateForgePathDisplay.ps1')
    if ($UpdateBaseline.IsPresent) {
        $outputRoot = Join-Path -Path $repoRoot -ChildPath 'performance-baselines'
    }
    else {
        $outputRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)
    }

    if (-not (Test-Path -LiteralPath $outputRoot)) {
        New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
    }

    $profiles = @(
        [PSCustomObject]@{ Name = 'small'; Sessions = 250; PayloadBytes = 512; Threads = 2 },
        [PSCustomObject]@{ Name = 'medium'; Sessions = 1000; PayloadBytes = 1024; Threads = 4 },
        [PSCustomObject]@{ Name = 'large'; Sessions = 3000; PayloadBytes = 4096; Threads = 8 }
    )

    if ($Profile -ne 'All') {
        $profiles = @($profiles | Where-Object { $_.Name -eq $Profile.ToLowerInvariant() })
    }

    $results = @()
    foreach ($item in $profiles) {
        $jsonPath = Join-Path -Path $outputRoot -ChildPath ($item.Name + '.json')
        $csvPath = Join-Path -Path $outputRoot -ChildPath ($item.Name + '.csv')

        & (Join-Path -Path $PSScriptRoot -ChildPath 'Invoke-StateForgeScaleTest.ps1') `
            -Sessions $item.Sessions `
            -PayloadBytes $item.PayloadBytes `
            -Threads $item.Threads `
            -ExportJson $jsonPath `
            -ExportCsv $csvPath | Out-Host

        if (-not $?) {
            throw "Performance profile failed: $($item.Name)"
        }

        $results += [PSCustomObject]@{
            Profile      = $item.Name
            Sessions     = $item.Sessions
            PayloadBytes = $item.PayloadBytes
            Threads      = $item.Threads
            CsvPath      = ConvertTo-StateForgeDisplayPath -Path $csvPath -RepositoryRoot $repoRoot
            JsonPath     = ConvertTo-StateForgeDisplayPath -Path $jsonPath -RepositoryRoot $repoRoot
            Updated      = [bool]$UpdateBaseline
        }
    }

    $results
}
catch {
    Write-Error -ErrorRecord $_
}
