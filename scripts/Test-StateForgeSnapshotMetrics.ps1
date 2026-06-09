<#
.SYNOPSIS
Runs StateForge snapshot-backed metrics validation.

.DESCRIPTION
Validates snapshot capture, snapshot read, and snapshot-backed Prometheus output.

.EXAMPLE
.\scripts\Test-StateForgeSnapshotMetrics.ps1

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
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.SnapshotTests\StateForge.SnapshotTests.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge snapshot metrics tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
