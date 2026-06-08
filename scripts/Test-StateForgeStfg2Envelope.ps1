<#
.SYNOPSIS
Runs the StateForge STFG2 envelope harness.

.DESCRIPTION
Validates STFG2 wrap/unwrap behavior and STFG1 compatibility passthrough.

.EXAMPLE
.\scripts\Test-StateForgeStfg2Envelope.ps1

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.FormatHarness\StateForge.FormatHarness.csproj'

    & dotnet run --project $projectPath --configuration Release

    if ($LASTEXITCODE -ne 0) {
        throw "StateForge STFG2 envelope harness failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        Project = $projectPath
        Success = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
