<#
.SYNOPSIS
Runs StateForge maintenance jobs.

.DESCRIPTION
Runs cleanup, health, and statistics maintenance through the StateForge.Maintenance project.

.PARAMETER RootPath
StateForge root path.

.PARAMETER Once
Job to run: all, cleanup, health, or stats.

.EXAMPLE
.\scripts\Invoke-StateForgeMaintenance.ps1 -RootPath ..\StateForgeSmoke\demo -Once all

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
    [ValidateSet('all', 'cleanup', 'health', 'stats')]
    [string]$Once = 'all'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Maintenance\StateForge.Maintenance.csproj'
    $resolvedRoot = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RootPath)

    & dotnet run --project $projectPath --configuration Release -- --root $resolvedRoot --once $Once

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge maintenance failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        RootPath = $resolvedRoot
        Once     = $Once
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
