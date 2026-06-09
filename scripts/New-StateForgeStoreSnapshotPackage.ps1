[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceRootPath,

    [Parameter(Mandatory = $true)]
    [string]$SnapshotRepositoryPath,

    [Parameter()]
    [string]$SnapshotName
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    Write-Host "Use StateForge.Snapshots StateForgeSnapshotService.Create() for operational snapshot creation."
    [PSCustomObject]@{
        SourceRootPath         = $SourceRootPath
        SnapshotRepositoryPath = $SnapshotRepositoryPath
        SnapshotName           = $SnapshotName
        Success                = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
