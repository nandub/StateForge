<#
.SYNOPSIS
Lists StateForge session files.

.DESCRIPTION
Uses StateForge.Tools to list stored session metadata.

.PARAMETER RootPath
The StateForge root path.

.EXAMPLE
.\scripts\Get-StateForgeSession.ps1 -RootPath D:\StateForge

.INPUTS
None.

.OUTPUTS
System.String.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
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

    & dotnet run --project $toolProject -- list --root $RootPath
    if ($LASTEXITCODE -ne 0) {
        throw "StateForge.Tools list failed with exit code $LASTEXITCODE."
    }
}
catch {
    Write-Error -ErrorRecord $_
}
