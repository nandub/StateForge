<#
.SYNOPSIS
Validates built StateForge NuGet and symbol package artifacts.

.DESCRIPTION
Runs the package validation harness against nupkg and snupkg files, including
NuGet metadata, repository commit, portable PDB, and SourceLink mappings.

.PARAMETER PackagePath
Directory containing package artifacts.

.PARAMETER Version
Expected package version.

.PARAMETER RepositoryCommit
Expected 40-character Git commit.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [Parameter()]
    [string]$Version = '0.35.0',

    [Parameter()]
    [string]$RepositoryCommit
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $resolvedPackagePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PackagePath)

    if (-not (Test-Path -LiteralPath $resolvedPackagePath -PathType Container)) {
        throw "Package directory not found: $resolvedPackagePath"
    }

    if ([string]::IsNullOrWhiteSpace($RepositoryCommit)) {
        $RepositoryCommit = (& git -C $repoRoot rev-parse HEAD).Trim()
        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to resolve the repository commit.'
        }
    }

    $projectPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.PackageValidationTests\StateForge.PackageValidationTests.csproj'
    & dotnet run --project $projectPath --configuration Release -- $resolvedPackagePath $Version $RepositoryCommit
    if ($LASTEXITCODE -ne 0) {
        throw "Package artifact validation failed with exit code $LASTEXITCODE."
    }

    [PSCustomObject]@{
        PackagePath      = $resolvedPackagePath
        Version          = $Version
        RepositoryCommit = $RepositoryCommit
        Success          = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
