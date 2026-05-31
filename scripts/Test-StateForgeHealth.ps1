<#
.SYNOPSIS
Runs StateForge store health checks.

.DESCRIPTION
Uses StateForge.Tools to validate configuration and run read/write/lock/enumerate/cleanup health checks.

.PARAMETER RootPath
StateForge root path.

.EXAMPLE
.\scripts\Test-StateForgeHealth.ps1 -RootPath ..\StateForgeFarm

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
    [string]$RootPath
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Tools\StateForge.Tools.csproj'

    & dotnet run --project $projectPath -- validate --root $RootPath
    if ($LASTEXITCODE -ne 0) {
        throw "StateForge validation failed."
    }

    & dotnet run --project $projectPath -- health --root $RootPath
    if ($LASTEXITCODE -ne 0) {
        throw "StateForge health check failed."
    }

    [PSCustomObject]@{
        RootPath = $RootPath
        Success  = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
