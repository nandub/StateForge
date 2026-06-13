<#
.SYNOPSIS
Runs StateForge incremental snapshot tests.

.DESCRIPTION
Validates base snapshot creation, incremental delta detection, restore-chain replay, deleted file replay, and manifest parsing.

.EXAMPLE
.\scripts\Test-StateForgeIncrementalSnapshots.ps1

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
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.IncrementalSnapshotTests\StateForge.IncrementalSnapshotTests.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge incremental snapshot tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
