<#
.SYNOPSIS
Checks NuGet sources for StateForge restore.

.DESCRIPTION
Displays configured dotnet NuGet sources and verifies that the repository NuGet.config exists.

.EXAMPLE
.\scripts\Test-NuGetSources.ps1

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
    $nugetConfig = Join-Path -Path $repoRoot -ChildPath 'NuGet.config'

    if (-not (Test-Path -LiteralPath $nugetConfig)) {
        throw "NuGet.config not found: $nugetConfig"
    }

    Write-Verbose "Using NuGet.config: $nugetConfig"

    & dotnet nuget list source --configfile $nugetConfig
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet nuget list source failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        RepositoryRoot = $repoRoot
        NuGetConfig    = $nugetConfig
        Success        = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
