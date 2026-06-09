<#
.SYNOPSIS
Runs the StateForge replication host once.

.DESCRIPTION
Runs StateForge.Replication.Host with primary and replica root paths.

.PARAMETER PrimaryRootPath
Primary StateForge root path.

.PARAMETER ReplicaRootPath
One or more replica root paths.

.PARAMETER ManifestPath
Optional manifest output path.

.PARAMETER DryRun
Performs planning and manifest generation without copying files.

.EXAMPLE
.\scripts\Start-StateForgeReplicationHost.ps1 -PrimaryRootPath D:\StateForgePrimary -ReplicaRootPath D:\ReplicaA,D:\ReplicaB -ManifestPath .\replication.json -DryRun

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
    [string]$PrimaryRootPath,

    [Parameter(Mandatory = $true)]
    [string[]]$ReplicaRootPath,

    [Parameter()]
    [string]$ManifestPath,

    [Parameter()]
    [switch]$DryRun
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $projectPath = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.Replication.Host\StateForge.Replication.Host.csproj'
    $primary = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PrimaryRootPath)
    $replicas = @()

    foreach ($path in $ReplicaRootPath) {
        $replicas += $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($path)
    }

    $arguments = @(
        'run',
        '--project',
        $projectPath,
        '--configuration',
        'Release',
        '--',
        '--primary',
        $primary,
        '--replicas',
        ($replicas -join ';')
    )

    if (-not [string]::IsNullOrWhiteSpace($ManifestPath)) {
        $arguments += '--manifest'
        $arguments += $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ManifestPath)
    }

    if ($DryRun.IsPresent) {
        $arguments += '--dry-run'
    }

    & dotnet @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge replication host failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        PrimaryRootPath = $primary
        ReplicaRootPath = $replicas
        ManifestPath    = $ManifestPath
        DryRun          = $DryRun.IsPresent
        Success         = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
