<#
.SYNOPSIS
Runs the StateForge recursive STFG2 store migration harness.

.DESCRIPTION
Validates recursive store migration dry-run and apply behavior.

.EXAMPLE
.\scripts\Test-StateForgeStfg2StoreMigration.ps1

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
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.StoreMigrationHarness\StateForge.StoreMigrationHarness.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge STFG2 store migration harness failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
