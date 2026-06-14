<#
.SYNOPSIS
Builds the StateForge documentation site and generated .NET API reference.

.DESCRIPTION
Restores the repository-pinned DocFX tool, extracts metadata from all shipped package
projects, and builds the combined conceptual and generated API documentation site.

.EXAMPLE
.\scripts\Build-StateForgeApiDocs.ps1

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
    $repositoryRoot = Split-Path -Path $PSScriptRoot -Parent
    $configurationPath = Join-Path -Path $repositoryRoot -ChildPath 'docfx.json'
    $sitePath = Join-Path -Path $repositoryRoot -ChildPath 'artifacts\docfx\site'

    if (-not (Test-Path -LiteralPath $configurationPath)) {
        throw "DocFX configuration not found: $configurationPath"
    }

    Push-Location -LiteralPath $repositoryRoot
    try {
        & dotnet tool restore
        if ($LASTEXITCODE -ne 0) {
            throw 'dotnet tool restore failed.'
        }

        & dotnet tool run docfx $configurationPath
        if ($LASTEXITCODE -ne 0) {
            throw 'DocFX build failed.'
        }
    }
    finally {
        Pop-Location
    }

    $indexPath = Join-Path -Path $sitePath -ChildPath 'README.html'
    if (-not (Test-Path -LiteralPath $indexPath)) {
        throw "Generated documentation home page not found: $indexPath"
    }

    [PSCustomObject]@{
        Configuration = $configurationPath
        SitePath      = $sitePath
        Success       = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
