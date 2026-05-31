<#
.SYNOPSIS
Runs StateForge local smoke tests without IIS.

.DESCRIPTION
Runs a console smoke-test harness that validates StateForge FileStore behavior, persistence across store recreation, compression, encryption, locking, expiration cleanup, corruption quarantine, and ASP.NET Core IDistributedCache adapter behavior.

.PARAMETER RootPath
Optional root path for smoke-test data. If omitted, the smoke-test executable uses a temporary path.

.PARAMETER Configuration
Build configuration. Defaults to Release.

.PARAMETER Keep
Keep smoke-test files after execution.

.EXAMPLE
.\scripts\Invoke-StateForgeSmokeTest.ps1

.EXAMPLE
.\scripts\Invoke-StateForgeSmokeTest.ps1 -RootPath D:\StateForgeSmoke -Keep

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
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [switch]$Keep,

    [Parameter()]
    [switch]$SkipDemo
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.SmokeTests\StateForge.SmokeTests.csproj'

    $arguments = @(
        'run',
        '--project',
        $projectPath,
        '--configuration',
        $Configuration,
        '--'
    )

    if (-not [string]::IsNullOrWhiteSpace($RootPath)) {
        $arguments += '--root'
        $arguments += $RootPath
    }

    if ($Keep.IsPresent) {
        $arguments += '--keep'
    }

    if ($SkipDemo.IsPresent) {
        $arguments += '--skip-demo'
    }

    & dotnet @arguments

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge smoke tests failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project       = $projectPath
        Configuration = $Configuration
        RootPath      = $RootPath
        Success       = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
