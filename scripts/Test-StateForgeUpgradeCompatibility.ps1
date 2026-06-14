<#
.SYNOPSIS
Runs StateForge rolling-upgrade compatibility validation.

.DESCRIPTION
Validates deterministic legacy STFG1 fixtures against the current FileStore,
replication, snapshot, sharding migration, and documented downgrade boundaries.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.UpgradeCompatibilityTests\StateForge.UpgradeCompatibilityTests.csproj'

    & dotnet run --project $projectPath --configuration Release
    if ($LASTEXITCODE -ne 0) {
        throw "StateForge upgrade compatibility tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
