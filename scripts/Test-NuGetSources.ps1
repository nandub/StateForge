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
    . (Join-Path -Path $scriptRoot -ChildPath 'StateForgePathDisplay.ps1')
    $nugetConfig = Join-Path -Path $repoRoot -ChildPath 'NuGet.config'

    if (-not (Test-Path -LiteralPath $nugetConfig)) {
        throw "NuGet.config not found: $(ConvertTo-StateForgeDisplayPath -Path $nugetConfig -RepositoryRoot $repoRoot)"
    }

    Write-Verbose "Using NuGet.config: $(ConvertTo-StateForgeDisplayPath -Path $nugetConfig -RepositoryRoot $repoRoot)"

    & dotnet nuget list source --configfile $nugetConfig
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet nuget list source failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        RepositoryRoot = ConvertTo-StateForgeDisplayPath -Path $repoRoot -RepositoryRoot $repoRoot
        NuGetConfig    = ConvertTo-StateForgeDisplayPath -Path $nugetConfig -RepositoryRoot $repoRoot
        Success        = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
