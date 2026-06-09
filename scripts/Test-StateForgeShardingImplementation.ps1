<#
.SYNOPSIS
Runs StateForge sharding implementation tests.

.DESCRIPTION
Validates sharded writes, sharded reads, legacy fallback reads, multi-depth remove, and shard analysis compatibility.

.EXAMPLE
.\scripts\Test-StateForgeShardingImplementation.ps1

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
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.ShardingTests\StateForge.ShardingTests.csproj'
    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge sharding implementation tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
