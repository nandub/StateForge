[CmdletBinding()]
param(
    [string]$RootPath,
    [string]$Config,
    [switch]$Once,
    [switch]$Loop,
    [int]$IntervalSeconds,
    [switch]$Json,
    [string]$LogPath,
    [switch]$CleanupOnly,
    [switch]$HealthOnly,
    [switch]$StatsOnly,
    [switch]$MigrationOnly,
    [switch]$ValidateConfig
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Maintenance.Host\StateForge.Maintenance.Host.csproj'

    $arguments = @('run', '--project', $projectPath, '--configuration', 'Release', '--')

    if (-not [string]::IsNullOrWhiteSpace($Config)) {
        $arguments += '--config'
        $arguments += $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Config)
    }

    if (-not [string]::IsNullOrWhiteSpace($RootPath)) {
        $arguments += '--root'
        $arguments += $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($RootPath)
    }

    if ($Loop.IsPresent) {
        $arguments += '--loop'
    } else {
        $arguments += '--once'
    }

    if ($IntervalSeconds -gt 0) {
        $arguments += '--interval-seconds'
        $arguments += [string]$IntervalSeconds
    }

    if ($Json.IsPresent) {
        $arguments += '--json'
    }


    if ($CleanupOnly.IsPresent) {
        $arguments += '--cleanup-only'
    }

    if ($HealthOnly.IsPresent) {
        $arguments += '--health-only'
    }

    if ($StatsOnly.IsPresent) {
        $arguments += '--stats-only'
    }

    if ($MigrationOnly.IsPresent) {
        $arguments += '--migration-only'
    }

    if ($ValidateConfig.IsPresent) {
        $arguments += '--validate-config'
    }

    if (-not [string]::IsNullOrWhiteSpace($LogPath)) {
        $arguments += '--log'
        $arguments += $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($LogPath)
    }

    & dotnet @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge Maintenance Host failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
