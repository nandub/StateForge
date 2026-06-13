<#
.SYNOPSIS
Tests StateForge replica lag monitoring.

.DESCRIPTION
Runs deterministic replica state, lag, stale detection, and Prometheus metric validation.

.EXAMPLE
.\scripts\Test-StateForgeReplicaMonitoring.ps1

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
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.ReplicaMonitoringTests\StateForge.ReplicaMonitoringTests.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge replica monitoring tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
