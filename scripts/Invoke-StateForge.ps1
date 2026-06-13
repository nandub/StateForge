<#
.SYNOPSIS
Runs StateForge operational helper commands.

.DESCRIPTION
Provides a consolidated entry point for operational StateForge script actions while preserving existing scripts.

.PARAMETER Command
Operational command to run.

.EXAMPLE
.\scripts\Invoke-StateForge.ps1 -Command BuildPackages

.EXAMPLE
.\scripts\Invoke-StateForge.ps1 -Command NewIncrementalSnapshot -Arguments @{ SourceRootPath='D:\StateForge'; SnapshotRepositoryPath='D:\Snapshots'; ParentSnapshotName='base'; SnapshotName='inc1' }

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
    [ValidateSet(
        'BuildPackages',
        'NewIncrementalSnapshot',
        'StartReplicationHost'
    )]
    [string]$Command,

    [Parameter()]
    [hashtable]$Arguments
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

function Invoke-CommandScript {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Required script not found: $Path"
    }

    if ($null -ne $Arguments -and $Arguments.Count -gt 0) {
        & $Path @Arguments
    }
    else {
        & $Path
    }
}

try {
    switch ($Command) {
        'BuildPackages' {
            Invoke-CommandScript -Path '.\scripts\Build-StateForgePackages.ps1'
        }

        'NewIncrementalSnapshot' {
            Invoke-CommandScript -Path '.\scripts\New-StateForgeIncrementalSnapshot.ps1'
        }

        'StartReplicationHost' {
            Invoke-CommandScript -Path '.\scripts\Start-StateForgeReplicationHost.ps1'
        }
    }

    [PSCustomObject]@{
        Command = $Command
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
