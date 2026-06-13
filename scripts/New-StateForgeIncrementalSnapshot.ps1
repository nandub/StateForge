<#
.SYNOPSIS
Creates an incremental StateForge snapshot.

.DESCRIPTION
Operational placeholder for StateForgeIncrementalSnapshotService until the CLI is expanded.

.PARAMETER SourceRootPath
StateForge source root path.

.PARAMETER SnapshotRepositoryPath
Snapshot repository path.

.PARAMETER ParentSnapshotName
Parent snapshot name.

.PARAMETER SnapshotName
New incremental snapshot name.

.EXAMPLE
.\scripts\New-StateForgeIncrementalSnapshot.ps1 -SourceRootPath D:\StateForge -SnapshotRepositoryPath D:\Snapshots -ParentSnapshotName base -SnapshotName inc1

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceRootPath,

    [Parameter(Mandatory = $true)]
    [string]$SnapshotRepositoryPath,

    [Parameter(Mandatory = $true)]
    [string]$ParentSnapshotName,

    [Parameter()]
    [string]$SnapshotName
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

[PSCustomObject]@{
    SourceRootPath         = $SourceRootPath
    SnapshotRepositoryPath = $SnapshotRepositoryPath
    ParentSnapshotName     = $ParentSnapshotName
    SnapshotName           = $SnapshotName
    UseService             = 'StateForge.Snapshots.StateForgeIncrementalSnapshotService'
    Success                = $true
}
