<#
.SYNOPSIS
Runs StateForge resilience tests.

.DESCRIPTION
Runs local resilience simulations including stale-lock recovery, store recreation, high-session-count statistics, and provider-style operations.

.PARAMETER RootPath
Test root path.

.PARAMETER Sessions
Number of sessions for the high-count test. Defaults to 10000.

.PARAMETER Keep
Keep test files.

.EXAMPLE
.\scripts\Invoke-StateForgeResilienceTest.ps1 -RootPath ..\StateForgeResilience -Sessions 10000 -Keep

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
    [int]$Sessions = 10000,

    [Parameter()]
    [switch]$Keep
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.ResilienceTests\StateForge.ResilienceTests.csproj'

    $arguments = @(
        'run',
        '--project',
        $projectPath,
        '--configuration',
        'Release',
        '--',
        '--sessions',
        $Sessions
    )

    if (-not [string]::IsNullOrWhiteSpace($RootPath)) {
        $arguments += '--root'
        $arguments += $RootPath
    }

    if ($Keep.IsPresent) {
        $arguments += '--keep'
    }

    & dotnet @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge resilience tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project  = $projectPath
        RootPath = $RootPath
        Sessions = $Sessions
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
