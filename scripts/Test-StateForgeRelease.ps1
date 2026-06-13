<#
.SYNOPSIS
Runs StateForge release-readiness validation.

.DESCRIPTION
Runs build, selected harnesses, maintenance host validation, and package artifact creation.

.PARAMETER PackageOutputPath
Directory where NuGet packages are written.

.EXAMPLE
.\scripts\Test-StateForgeRelease.ps1 -PackageOutputPath .\artifacts\nuget

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$PackageOutputPath = '.\artifacts\nuget'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    .\scripts\Build-StateForge.ps1
    .\scripts\Test-StateForgeFormat.ps1
    .\scripts\Test-StateForgeStfg2Envelope.ps1
    .\scripts\Test-StateForgeStfg2Migration.ps1
    .\scripts\Test-StateForgeStfg2StoreMigration.ps1
    .\scripts\Test-StateForgeMaintenanceHost.ps1
    .\scripts\Test-StateForgeMaintenanceTask.ps1
    .\scripts\Test-StateForgeObservability.ps1
    .\scripts\Test-StateForgeApiValidation.ps1
    .\scripts\Test-StateForgeScale.ps1
    .\scripts\Test-StateForgePerformance.ps1
    .\scripts\Test-StateForgeSnapshotMetrics.ps1
    .\scripts\Test-StateForgeReplication.ps1
    .\scripts\Test-StateForgeReplicationService.ps1
    .\scripts\Test-StateForgeReplicationManifest.ps1
    .\scripts\Test-StateForgeAutomaticFailover.ps1
    .\scripts\Test-StateForgeSnapshotMarkers.ps1
    .\scripts\Test-StateForgeHardening.ps1
    .\scripts\Test-StateForgeIncrementalSnapshots.ps1
    .\scripts\Test-StateForgeDocs.ps1
    .\scripts\Test-StateForgeVersionConsistency.ps1
    .\scripts\Test-StateForgeRecoveryFlow.ps1
    .\scripts\Test-StateForgeReplicaPromotion.ps1
    .\scripts\Test-StateForgeSnapshotScheduling.ps1
    .\scripts\Test-StateForgeSnapshotServices.ps1
    .\scripts\Build-StateForgePackages.ps1 -OutputPath $PackageOutputPath -Version '0.28.7'

    [PSCustomObject]@{
        PackageOutputPath = $PackageOutputPath
        Success           = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}



# Consolidated validation runner smoke checks.
.\scripts\Test-StateForge.ps1 -Suite Docs
.\scripts\Test-StateForge.ps1 -Suite Version
