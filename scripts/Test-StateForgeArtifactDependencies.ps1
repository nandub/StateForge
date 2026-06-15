<#
.SYNOPSIS
Validates that durable inputs are tracked outside the ignored artifacts directory.

.DESCRIPTION
Confirms that artifacts remains an output-only directory and that reviewed performance
baselines required by validation are stored under performance-baselines.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    $gitIgnorePath = Join-Path -Path $repoRoot -ChildPath '.gitignore'
    $baselineRoot = Join-Path -Path $repoRoot -ChildPath 'performance-baselines'
    $runnerPath = Join-Path -Path $PSScriptRoot -ChildPath 'Invoke-StateForgePerformanceBaseline.ps1'

    $gitIgnoreText = Get-Content -LiteralPath $gitIgnorePath -Raw
    if ($gitIgnoreText -notmatch '(?m)^artifacts[\\/]?\r?$') {
        throw '.gitignore must keep artifacts as generated output.'
    }

    $requiredInputs = @(
        'README.md',
        'small.csv',
        'small.json',
        'medium.csv',
        'medium.json',
        'large.csv',
        'large.json'
    )

    foreach ($requiredInput in $requiredInputs) {
        $path = Join-Path -Path $baselineRoot -ChildPath $requiredInput
        if (-not (Test-Path -LiteralPath $path)) {
            throw "Missing tracked performance input: $path"
        }
    }

    $runnerText = Get-Content -LiteralPath $runnerPath -Raw
    if ($runnerText -notmatch "artifacts\\performance" -or
        $runnerText -notmatch "performance-baselines") {
        throw 'Performance candidates must use artifacts and reviewed baselines must use performance-baselines.'
    }

    [PSCustomObject]@{
        IgnoredOutputPath = 'artifacts'
        TrackedInputPath  = 'performance-baselines'
        RequiredInputs    = $requiredInputs.Count
        Success           = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
