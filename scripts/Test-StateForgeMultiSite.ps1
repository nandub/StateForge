<#
.SYNOPSIS
Tests StateForge multi-site disaster recovery.

.DESCRIPTION
Runs deterministic site metadata, cross-site policy, restore drill, and fenced failover validation.

.EXAMPLE
.\scripts\Test-StateForgeMultiSite.ps1

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
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.MultiSiteTests\StateForge.MultiSiteTests.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge multi-site tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
