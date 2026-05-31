<#
.SYNOPSIS
Creates a StateForge storage folder and grants Modify rights.

.DESCRIPTION
Creates the StateForge root folder and grants Modify permissions to an IIS application pool identity.

.PARAMETER RootPath
StateForge root path.

.PARAMETER AppPoolName
IIS application pool name.

.EXAMPLE
.\scripts\Install-StateForgeStore.ps1 -RootPath D:\StateForge -AppPoolName MyAppPool -WhatIf

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$RootPath,

    [Parameter(Mandatory = $true)]
    [string]$AppPoolName
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $identity = "IIS AppPool\$AppPoolName"

    if ($PSCmdlet.ShouldProcess($RootPath, "Create StateForge root folder")) {
        New-Item -Path $RootPath -ItemType Directory -Force | Out-Null
    }

    if ($PSCmdlet.ShouldProcess($RootPath, "Grant Modify rights to $identity")) {
        & icacls $RootPath /grant "$($identity):(OI)(CI)(M)"
        if ($LASTEXITCODE -ne 0) {
            throw "icacls failed with exit code $LASTEXITCODE."
        }
    }

    [PSCustomObject]@{
        RootPath    = $RootPath
        Identity    = $identity
        Permissions = 'Modify'
        Success     = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
