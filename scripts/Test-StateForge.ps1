<#
.SYNOPSIS
Runs StateForge validation suites.

.DESCRIPTION
Consolidates StateForge validation into one suite-based runner. Existing feature-specific
Test-StateForge*.ps1 scripts remain available for compatibility.

.PARAMETER Suite
Validation suite to run.

.PARAMETER Configuration
Build configuration used by dotnet-based harnesses.

.EXAMPLE
.\scripts\Test-StateForge.ps1 -Suite Docs

.EXAMPLE
.\scripts\Test-StateForge.ps1 -Suite All

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
    [ValidateSet(
        'Docs',
        'Version',
        'Layout',
        'Source',
        'Format',
        'Migration',
        'Observability',
        'Maintenance',
        'Replication',
        'ReplicaCatchUp',
        'ReplicaMonitoring',
        'Quorum',
        'Witness',
        'SplitBrain',
        'MultiSite',
        'Snapshots',
        'Recovery',
        'Hardening',
        'Release',
        'Production',
        'All'
    )]
    [string]$Suite = 'All',

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'
$script:StateForgeRepositoryRoot = Split-Path -Path $PSScriptRoot -Parent

function Invoke-StateForgeScript {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter()]
        [hashtable]$Arguments
    )

    $resolvedPath = $Path
    if (-not [System.IO.Path]::IsPathRooted($resolvedPath)) {
        $resolvedPath = Join-Path -Path $script:StateForgeRepositoryRoot -ChildPath $resolvedPath
    }

    if (-not (Test-Path -LiteralPath $resolvedPath)) {
        throw "Missing required validation script: $resolvedPath"
    }

    Write-Host "==> $resolvedPath"

    Push-Location -LiteralPath $script:StateForgeRepositoryRoot
    try {
        if ($null -ne $Arguments -and $Arguments.Count -gt 0) {
            & $resolvedPath @Arguments
        }
        else {
            & $resolvedPath
        }

        if (-not $?) {
            throw "$resolvedPath failed."
        }
    }
    finally {
        Pop-Location
    }
}


function Get-StateForgeProductionHealthRoot {
    [CmdletBinding()]
    param()

    $basePath = [System.IO.Path]::GetTempPath()
    return (Join-Path -Path $basePath -ChildPath 'StateForgeProductionHealth')
}

function Invoke-DocsSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeDocs.ps1'
}

function Invoke-VersionSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeVersionConsistency.ps1'
}

function Invoke-LayoutSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeLayout.ps1'
}

function Invoke-SourceSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeSource.ps1'
}

function Invoke-FormatSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeFormat.ps1'
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeStfg2Envelope.ps1'
}

function Invoke-MigrationSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeStfg2Migration.ps1'
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeStfg2StoreMigration.ps1'
}

function Invoke-ObservabilitySuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeObservability.ps1'
}

function Invoke-MaintenanceSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeMaintenanceHost.ps1'
}

function Invoke-ReplicationSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeReplication.ps1'
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeReplicationService.ps1'
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeReplicationManifest.ps1'
}


function Invoke-ReplicaCatchUpSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeReplicaCatchUp.ps1'
}

function Invoke-ReplicaMonitoringSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeReplicaMonitoring.ps1'
}

function Invoke-QuorumSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeQuorum.ps1'
}

function Invoke-WitnessSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeWitness.ps1'
}

function Invoke-SplitBrainSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeSplitBrain.ps1'
}

function Invoke-MultiSiteSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeMultiSite.ps1'
}

function Invoke-SnapshotsSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeSnapshotServices.ps1'
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeSnapshotScheduling.ps1'
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeSnapshotMarkers.ps1'
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeIncrementalSnapshots.ps1'
}

function Invoke-RecoverySuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeReplicaPromotion.ps1'
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeAutomaticFailover.ps1'
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeRecoveryFlow.ps1'
}

function Invoke-HardeningSuite {
    Invoke-DocsSuite
    Invoke-VersionSuite
    Invoke-LayoutSuite
    Invoke-SourceSuite
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeHardening.ps1'
}


function Invoke-ProductionSuite {
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeProductionNonInteractive.ps1'
    Invoke-DocsSuite
    Invoke-VersionSuite
    Invoke-LayoutSuite
    Invoke-SourceSuite
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgeHealth.ps1' -Arguments @{ RootPath = (Get-StateForgeProductionHealthRoot) }
    Invoke-StateForgeScript -Path '.\scripts\Invoke-StateForgeSmokeTest.ps1'
    Invoke-ObservabilitySuite
    Invoke-ReplicationSuite
    Invoke-ReplicaCatchUpSuite
    Invoke-ReplicaMonitoringSuite
    Invoke-QuorumSuite
    Invoke-WitnessSuite
    Invoke-SplitBrainSuite
    Invoke-MultiSiteSuite
    Invoke-SnapshotsSuite
    Invoke-RecoverySuite
    Invoke-StateForgeScript -Path '.\scripts\Test-StateForgePackageMetadata.ps1'
}

function Invoke-ReleaseSuite {
    Invoke-DocsSuite
    Invoke-VersionSuite
    Invoke-LayoutSuite
    Invoke-SourceSuite
    Invoke-FormatSuite
    Invoke-MigrationSuite
    Invoke-ObservabilitySuite
    Invoke-MaintenanceSuite
    Invoke-ReplicationSuite
    Invoke-ReplicaCatchUpSuite
    Invoke-ReplicaMonitoringSuite
    Invoke-QuorumSuite
    Invoke-WitnessSuite
    Invoke-SplitBrainSuite
    Invoke-MultiSiteSuite
    Invoke-SnapshotsSuite
    Invoke-RecoverySuite
}

try {
    switch ($Suite) {
        'Docs' { Invoke-DocsSuite }
        'Version' { Invoke-VersionSuite }
        'Layout' { Invoke-LayoutSuite }
        'Source' { Invoke-SourceSuite }
        'Format' { Invoke-FormatSuite }
        'Migration' { Invoke-MigrationSuite }
        'Observability' { Invoke-ObservabilitySuite }
        'Maintenance' { Invoke-MaintenanceSuite }
        'Replication' { Invoke-ReplicationSuite }
        'ReplicaCatchUp' { Invoke-ReplicaCatchUpSuite }
        'ReplicaMonitoring' { Invoke-ReplicaMonitoringSuite }
        'Quorum' { Invoke-QuorumSuite }
        'Witness' { Invoke-WitnessSuite }
        'SplitBrain' { Invoke-SplitBrainSuite }
        'MultiSite' { Invoke-MultiSiteSuite }
        'Snapshots' { Invoke-SnapshotsSuite }
        'Recovery' { Invoke-RecoverySuite }
        'Hardening' { Invoke-HardeningSuite }
        'Release' { Invoke-ReleaseSuite }
        'Production' { Invoke-ProductionSuite }
        'All' { Invoke-ReleaseSuite }
    }

    [PSCustomObject]@{
        Suite         = $Suite
        Configuration = $Configuration
        Success       = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
