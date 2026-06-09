<#
.SYNOPSIS
Runs StateForge end-to-end recovery-flow validation.

.DESCRIPTION
Validates replication, snapshot creation, restore, replica promotion, and automatic failover in one generated-store scenario.

.EXAMPLE
.\scripts\Test-StateForgeRecoveryFlow.ps1

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
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.RecoveryFlowTests\StateForge.RecoveryFlowTests.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge recovery-flow tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
