<#
.SYNOPSIS
Runs StateForge sharding migration validation.

.DESCRIPTION
Runs the sharding migration harness against a generated temporary store.

.EXAMPLE
.\scripts\Invoke-StateForgeShardingMigration.ps1

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
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.ShardingMigrationHarness\StateForge.ShardingMigrationHarness.csproj'
    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge sharding migration harness failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
