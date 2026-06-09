<#
.SYNOPSIS
Creates a StateForge store snapshot using StateForge.Tools.

.DESCRIPTION
Runs the StateForge.Tools snapshot command to capture a store snapshot JSON file.

.PARAMETER RootPath
StateForge root path.

.PARAMETER SnapshotPath
Snapshot output path.

.EXAMPLE
.\scripts\New-StateForgeSnapshot.ps1 -RootPath D:\StateForge -SnapshotPath .\artifacts\snapshots\store.json

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
    [string]$RootPath,

    [Parameter()]
    [string]$SnapshotPath = '.\artifacts\snapshots\stateforge-store-snapshot.json'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $toolProject = Join-Path -Path (Get-Location) -ChildPath 'src\StateForge.Tools\StateForge.Tools.csproj'
    $resolvedRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RootPath)
    $resolvedSnapshot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($SnapshotPath)

    & dotnet run --project $toolProject --configuration Release -- snapshot --root $resolvedRoot --snapshot $resolvedSnapshot

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge snapshot command failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        RootPath     = $resolvedRoot
        SnapshotPath = $resolvedSnapshot
        Success      = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
