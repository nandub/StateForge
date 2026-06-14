<#
.SYNOPSIS
Validates the generated StateForge .NET API documentation.

.DESCRIPTION
Builds the DocFX site, verifies that every shipped package namespace appears in generated
metadata, and confirms that foundational packages enforce missing XML comments as errors.

.EXAMPLE
.\scripts\Test-StateForgeApiDocs.ps1

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
    $buildScript = Join-Path -Path $PSScriptRoot -ChildPath 'Build-StateForgeApiDocs.ps1'
    $metadataRoot = Join-Path -Path $repositoryRoot -ChildPath 'artifacts\docfx\api'
    $siteRoot = Join-Path -Path $repositoryRoot -ChildPath 'artifacts\docfx\site'
    $targetsPath = Join-Path -Path $repositoryRoot -ChildPath 'Directory.Build.targets'

    & $buildScript
    if (-not $?) {
        throw 'API documentation build failed.'
    }

    $metadata = Get-ChildItem -LiteralPath $metadataRoot -Filter '*.yml' -Recurse |
        ForEach-Object { Get-Content -LiteralPath $_.FullName -Raw }
    $metadataText = $metadata -join [Environment]::NewLine

    $requiredNamespaces = @(
        'StateForge.Core',
        'StateForge.FileStore',
        'StateForge.AspNet',
        'StateForge.AspNetCore',
        'StateForge.Security',
        'StateForge.Telemetry',
        'StateForge.CloudNative',
        'StateForge.Format',
        'StateForge.Prometheus',
        'StateForge.Performance',
        'StateForge.Replication',
        'StateForge.Snapshots'
    )

    foreach ($requiredNamespace in $requiredNamespaces) {
        if ($metadataText -notmatch [regex]::Escape($requiredNamespace)) {
            throw "Generated API metadata is missing namespace: $requiredNamespace"
        }
    }

    $targetsText = Get-Content -LiteralPath $targetsPath -Raw
    foreach ($documentedProject in @('StateForge.Core', 'StateForge.Format', 'StateForge.Security')) {
        if ($targetsText -notmatch [regex]::Escape($documentedProject)) {
            throw "Missing XML documentation enforcement for project: $documentedProject"
        }
    }

    if ($targetsText -notmatch 'GenerateDocumentationFile' -or
        $targetsText -notmatch 'WarningsAsErrors') {
        throw 'Package XML documentation generation or foundational coverage enforcement is missing.'
    }

    $apiIndexPath = Join-Path -Path $siteRoot -ChildPath 'api\index.html'
    $rootTocPath = Join-Path -Path $siteRoot -ChildPath 'toc.html'
    $apiTocPath = Join-Path -Path $siteRoot -ChildPath 'api\toc.html'

    foreach ($navigationPath in @($apiIndexPath, $rootTocPath, $apiTocPath)) {
        if (-not (Test-Path -LiteralPath $navigationPath)) {
            throw "Generated API navigation file is missing: $navigationPath"
        }
    }

    $rootTocText = Get-Content -LiteralPath $rootTocPath -Raw
    $apiTocText = Get-Content -LiteralPath $apiTocPath -Raw

    if ($rootTocText -notmatch 'href="api/index\.html"') {
        throw 'The main documentation navigation does not link to the generated API reference.'
    }

    foreach ($requiredNamespace in $requiredNamespaces) {
        if ($apiTocText -notmatch [regex]::Escape($requiredNamespace + '.html')) {
            throw "Generated API navigation is missing namespace link: $requiredNamespace"
        }
    }

    [PSCustomObject]@{
        PackageNamespaces = $requiredNamespaces.Count
        EnforcedProjects  = 3
        Success           = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
