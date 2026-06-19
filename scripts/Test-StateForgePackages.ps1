<#
.SYNOPSIS
Builds and validates StateForge release packages.

.DESCRIPTION
Builds packages in a temporary directory, validates metadata and SourceLink
artifacts, and compiles isolated package consumer projects.

.PARAMETER Version
Package version to validate.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$Version = '1.0.0'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $packageRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ('StateForgePackages-' + [Guid]::NewGuid().ToString('N'))
    New-Item -Path $packageRoot -ItemType Directory -Force | Out-Null

    try {
        & (Join-Path $PSScriptRoot 'Test-StateForgePackageMetadata.ps1') | Out-Host
        & (Join-Path $PSScriptRoot 'Build-StateForgePackages.ps1') -OutputPath $packageRoot -Version $Version | Out-Host
        & (Join-Path $PSScriptRoot 'Test-StateForgePackageArtifacts.ps1') -PackagePath $packageRoot -Version $Version | Out-Host
        & (Join-Path $PSScriptRoot 'Test-StateForgePackageInstall.ps1') -PackagePath $packageRoot -Version $Version | Out-Host

        [PSCustomObject]@{
            Version = $Version
            Success = $true
        }
    }
    finally {
        if (Test-Path -LiteralPath $packageRoot) {
            Remove-Item -LiteralPath $packageRoot -Recurse -Force
        }
    }
}
catch {
    Write-Error -ErrorRecord $_
}
