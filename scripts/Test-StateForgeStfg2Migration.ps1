<#
.SYNOPSIS
Runs the StateForge STFG2 migration harness.

.DESCRIPTION
Validates migration of a legacy payload file into an STFG2 envelope and confirms existing STFG2 files are passed through.

.EXAMPLE
.\scripts\Test-StateForgeStfg2Migration.ps1

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
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.MigrationHarness\StateForge.MigrationHarness.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge STFG2 migration harness failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
