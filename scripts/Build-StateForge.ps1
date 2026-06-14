<#
.SYNOPSIS
Builds the StateForge solution.

.DESCRIPTION
Validates repository layout, validates the solution file, validates NuGet sources, restores packages, and builds the StateForge solution using the dotnet CLI.

.PARAMETER Configuration
Build configuration. Defaults to Release.

.EXAMPLE
.\scripts\Build-StateForge.ps1 -Configuration Release -WhatIf

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Requires the .NET SDK. Compatible with Windows PowerShell 5.1.

Important:
This script intentionally does not check $LASTEXITCODE after invoking helper PowerShell scripts. $LASTEXITCODE is only reliable for native/external commands such as dotnet.exe.
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
    $docsValidationScript = Join-Path -Path $scriptRoot -ChildPath 'Test-StateForgeDocs.ps1'
    $nugetSourceScript = Join-Path -Path $scriptRoot -ChildPath 'Test-NuGetSources.ps1'

    if (-not (Test-Path -LiteralPath $solutionPath)) {
        throw "Solution file not found: $solutionPath"
    }

    & $layoutScript | Out-Host
    & $solutionValidationScript | Out-Host
    & $sourceValidationScript | Out-Host
    & $docsValidationScript | Out-Host
    & $nugetSourceScript | Out-Host

    if ($PSCmdlet.ShouldProcess($solutionPath, "Restore and build StateForge solution")) {
        & dotnet restore $solutionPath --configfile $nugetConfig
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet restore failed with exit code $LASTEXITCODE."
        }

        & dotnet build $solutionPath --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet build failed with exit code $LASTEXITCODE."
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

# v0.30.3 docs consolidated model: Build validation should use Test-StateForgeDocs.ps1

# Consolidated docs required by Test-StateForgeDocs.ps1
