<#
.SYNOPSIS
Runs the StateForge classic ASP.NET provider harness.

.DESCRIPTION
Runs direct SessionStateStoreProviderBase lifecycle operations without IIS.

.PARAMETER RootPath
Harness root path.

.PARAMETER Keep
Keep generated session files.

.EXAMPLE
.\scripts\Invoke-StateForgeAspNetHarness.ps1 -RootPath ..\StateForgeAspNetHarness -Keep

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$RootPath,

    [Parameter()]
    [switch]$Keep
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.AspNetHarness\StateForge.AspNetHarness.csproj'

    $arguments = @('run', '--project', $projectPath, '--configuration', 'Release', '--')

    if (-not [string]::IsNullOrWhiteSpace($RootPath)) {
        $resolvedRootPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RootPath)
        $arguments += '--root'
        $arguments += $resolvedRootPath
    }

    if ($Keep.IsPresent) {
        $arguments += '--keep'
    }

    & dotnet @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge ASP.NET harness failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project  = $projectPath
        RootPath = $RootPath
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
