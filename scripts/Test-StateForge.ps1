<#
.SYNOPSIS
Runs StateForge tests.

.DESCRIPTION
Validates repository layout and solution file, restores packages, then runs dotnet test against the StateForge solution.

.PARAMETER Configuration
Test configuration. Defaults to Release.

.EXAMPLE
.\scripts\Test-StateForge.ps1 -Configuration Release -WhatIf

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Requires the .NET SDK. Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $solutionPath = Join-Path -Path $repoRoot -ChildPath 'StateForge.sln'
    $nugetConfig = Join-Path -Path $repoRoot -ChildPath 'NuGet.config'
    $layoutScript = Join-Path -Path $scriptRoot -ChildPath 'Test-StateForgeLayout.ps1'
    $solutionValidationScript = Join-Path -Path $scriptRoot -ChildPath 'Test-StateForgeSolution.ps1'
    $sourceValidationScript = Join-Path -Path $scriptRoot -ChildPath 'Test-StateForgeSource.ps1'

    & $layoutScript | Out-Host
    & $solutionValidationScript | Out-Host
    & $sourceValidationScript | Out-Host

    if ($PSCmdlet.ShouldProcess($solutionPath, "Run StateForge tests")) {
        & dotnet restore $solutionPath --configfile $nugetConfig
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore failed with exit code $LASTEXITCODE."
        }

        & dotnet test $solutionPath --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet test failed with exit code $LASTEXITCODE."
        }
    }

    [PSCustomObject]@{
        Solution      = $solutionPath
        Configuration = $Configuration
        Success       = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
