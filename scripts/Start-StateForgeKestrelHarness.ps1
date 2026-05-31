<#
.SYNOPSIS
Starts the StateForge Kestrel harness.

.DESCRIPTION
Starts a local ASP.NET Core Kestrel host that uses StateForge as an IDistributedCache backend.

.PARAMETER RootPath
Harness root path.

.PARAMETER Url
Kestrel URL. Defaults to http://localhost:5075.

.EXAMPLE
.\scripts\Start-StateForgeKestrelHarness.ps1 -RootPath ..\StateForgeKestrel -Url http://localhost:5075

.INPUTS
None.

.OUTPUTS
None.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$RootPath,

    [Parameter()]
    [string]$Url = 'http://localhost:5075'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.KestrelHarness\StateForge.KestrelHarness.csproj'

$env:ASPNETCORE_URLS = $Url

$arguments = @('run', '--project', $projectPath, '--configuration', 'Release', '--')

if (-not [string]::IsNullOrWhiteSpace($RootPath)) {
    $resolvedRootPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RootPath)
    $arguments += '--root'
    $arguments += $resolvedRootPath
}

& dotnet @arguments
