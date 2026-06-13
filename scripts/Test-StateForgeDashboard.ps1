<#
.SYNOPSIS
Tests the StateForge dashboard command.

.DESCRIPTION
Runs the StateForge.Tools dashboard command against a temporary store.

.EXAMPLE
.\scripts\Test-StateForgeDashboard.ps1

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
    $repoRoot = Split-Path -Path $PSScriptRoot -Parent
    $root = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath 'StateForgeDashboardTest'

    if (Test-Path -LiteralPath $root) {
        Remove-Item -LiteralPath $root -Recurse -Force
    }

    New-Item -Path $root -ItemType Directory -Force | Out-Null

    $replicaRoot = Join-Path -Path $root -ChildPath 'replica-west'
    New-Item -Path $replicaRoot -ItemType Directory -Force | Out-Null
    $now = [DateTimeOffset]::UtcNow.ToString('o')
    $replicaState = [ordered]@{
        version               = '1'
        replicaName           = 'west'
        replicaRootPath       = $replicaRoot
        lastAttemptUtc        = $now
        lastSuccessfulSyncUtc = $now
        catchUpOperations     = 2
        failedSyncs           = 0
        lastError             = ''
    } | ConvertTo-Json
    $statePath = Join-Path -Path $replicaRoot -ChildPath 'stateforge-replica-state.json'
    [System.IO.File]::WriteAllText($statePath, $replicaState, (New-Object System.Text.UTF8Encoding($false)))

    $toolProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Tools\StateForge.Tools.csproj'
    $replicaConfiguration = 'west=' + $replicaRoot
    $output = & dotnet run --project $toolProject --configuration Release --no-build -- dashboard --root $root --replicas $replicaConfiguration --replica-stale-seconds 300

    if ($LASTEXITCODE -ne 0) {
        throw "Dashboard command failed with exit code $LASTEXITCODE."
    }

    $text = ($output | Out-String)

    if ($text -notmatch 'StateForge Dashboard') {
        throw "Dashboard header was not found."
    }

    if ($text -notmatch 'Health') {
        throw "Dashboard health section was not found."
    }

    if ($text -notmatch 'Replicas' -or $text -notmatch 'west' -or $text -notmatch 'HEALTHY') {
        throw "Dashboard replica health output was not found."
    }

    [PSCustomObject]@{
        RootPath = $root
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
