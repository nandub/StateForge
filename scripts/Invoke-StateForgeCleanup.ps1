<#
.SYNOPSIS
Cleans expired or invalid StateForge session files.

.DESCRIPTION
Uses StateForge.Tools to clean expired entries. Invalid files are quarantined by default.

.PARAMETER RootPath
The StateForge root path.

.EXAMPLE
.\scripts\Invoke-StateForgeCleanup.ps1 -RootPath D:\StateForge -WhatIf

.INPUTS
None.

.OUTPUTS
System.String.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$RootPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $toolProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Tools\StateForge.Tools.csproj'

    if ($PSCmdlet.ShouldProcess($RootPath, 'Clean expired StateForge sessions')) {
        & dotnet run --project $toolProject -- cleanup --root $RootPath
        if ($LASTEXITCODE -ne 0) {
            throw "StateForge.Tools cleanup failed with exit code $LASTEXITCODE."
        }
    }
}
catch {
    Write-Error -ErrorRecord $_
}
