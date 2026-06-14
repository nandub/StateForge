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

    [PSCustomObject]@{
        PackageNamespaces = $requiredNamespaces.Count
        EnforcedProjects  = 3
        Success           = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
