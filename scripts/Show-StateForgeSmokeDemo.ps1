<#
.SYNOPSIS
Shows StateForge smoke-test demo store diagnostics, list, and stats.

.DESCRIPTION
Runs AES-aware StateForge.Tools commands against a smoke-test demo store.

.PARAMETER RootPath
Smoke-test root path, not the demo subfolder. Example: ..\StateForgeSmokeFresh3

.PARAMETER AesKeyBase64
AES key used by the smoke-test demo. Defaults to the deterministic smoke-test key.

.EXAMPLE
.\scripts\Show-StateForgeSmokeDemo.ps1 -RootPath ..\StateForgeSmokeFresh3

.INPUTS
None.

.OUTPUTS
None.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$RootPath,

    [Parameter()]
    [string]$AesKeyBase64 = 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA='
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$toolsProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Tools\StateForge.Tools.csproj'
$demoRoot = Join-Path -Path $RootPath -ChildPath 'demo'

Write-Host 'Diagnostics'
Write-Host '-----------'
& dotnet run --project $toolsProject -- diag --root $demoRoot

Write-Host ''
Write-Host 'List with AES key'
Write-Host '-----------------'
& dotnet run --project $toolsProject -- list --root $demoRoot --format json --protection aes --aes-key $AesKeyBase64

Write-Host ''
Write-Host 'Stats with AES key'
Write-Host '------------------'
& dotnet run --project $toolsProject -- stats --root $demoRoot --format json --protection aes --aes-key $AesKeyBase64
