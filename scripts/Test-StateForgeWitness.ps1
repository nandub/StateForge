<#
.SYNOPSIS
Tests StateForge witness node foundations.

.DESCRIPTION
Runs deterministic witness persistence, health, vote, and quorum integration validation.

.EXAMPLE
.\scripts\Test-StateForgeWitness.ps1

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
    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.WitnessTests\StateForge.WitnessTests.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge witness tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
