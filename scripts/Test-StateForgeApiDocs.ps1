<#
.SYNOPSIS
Validates the generated StateForge .NET API documentation.

.DESCRIPTION
Builds the DocFX site, verifies that every shipped package namespace appears in generated
metadata, and confirms that every shipped package enforces missing XML comments as errors.

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
    $documentedProjects = @(
        'StateForge.AspNet',
        'StateForge.AspNetCore',
        'StateForge.CloudNative',
        'StateForge.Core',
        'StateForge.FileStore',
        'StateForge.Format',
        'StateForge.Performance',
        'StateForge.Prometheus',
        'StateForge.Replication',
        'StateForge.Security',
        'StateForge.Snapshots',
        'StateForge.Telemetry'
    )

    foreach ($documentedProject in $documentedProjects) {
        $projectPath = Join-Path -Path $repositoryRoot -ChildPath (
            'src\' + $documentedProject + '\' + $documentedProject + '.csproj')
        $projectText = Get-Content -LiteralPath $projectPath -Raw
        if ($projectText -notmatch 'PackageReadmeFile') {
            throw "Shipped project is not covered by package XML documentation enforcement: $documentedProject"
        }
    }

    if ($targetsText -notmatch 'GenerateDocumentationFile' -or
        $targetsText -notmatch 'DocumentationFile' -or
        $targetsText -notmatch 'WarningsAsErrors' -or
        $targetsText -notmatch '1591' -or
        $targetsText -match '<NoWarn>[^<]*1591') {
        throw 'Package XML documentation generation or complete coverage enforcement is missing.'
    }

    $apiIndexPath = Join-Path -Path $siteRoot -ChildPath 'api\index.html'
    $apiGuidePath = Join-Path -Path $siteRoot -ChildPath '08-api-reference.html'
    $rootTocPath = Join-Path -Path $siteRoot -ChildPath 'toc.html'
    $apiTocPath = Join-Path -Path $siteRoot -ChildPath 'api\toc.html'

    foreach ($navigationPath in @($apiIndexPath, $apiGuidePath, $rootTocPath, $apiTocPath)) {
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

    $apiGuideText = Get-Content -LiteralPath $apiGuidePath -Raw
    foreach ($requiredApiGuideLink in @(
        'href="index\.html"><code>artifacts\\docfx\\site\\index\.html</code>',
        'href="api/index\.html">Generated \.NET API</a>',
        'href="api/StateForge\.FileStore\.StateForgeFileStore\.html"',
        'href="api/StateForge\.Snapshots\.StateForgeSnapshotService\.html"',
        'StateForgeReplicaMonitor_Capture_'
    )) {
        if ($apiGuideText -notmatch $requiredApiGuideLink) {
            throw "The generated API guide is missing a required clickable reference: $requiredApiGuideLink"
        }
    }

    $requiredExamples = @(
        @{
            Page = 'api\StateForge.FileStore.StateForgeFileStore.html'
            Text = 'Create a store, write a UTF-8 value, and read it back'
        },
        @{
            Page = 'api\StateForge.FileStore.StateForgeFileStore.html'
            Text = 'Use the returned lock ID as a fencing token when updating'
        },
        @{
            Page = 'api\StateForge.FileStore.StateForgeStfg2Migrator.html'
            Text = 'Migrate one legacy record while preserving the source'
        },
        @{
            Page = 'api\StateForge.AspNetCore.StateForgeServiceCollectionExtensions.html'
            Text = 'Register StateForge before adding ASP.NET Core session services'
        },
        @{
            Page = 'api\StateForge.AspNet.StateForgeSessionStateProvider.html'
            Text = 'Register the provider for out-of-process durable session state'
        },
        @{
            Page = 'api\StateForge.CloudNative.StateForgeCloudNativeExtensions.html'
            Text = 'Configure a minimal cloud-native application'
        },
        @{
            Page = 'api\StateForge.Format.StateForgeStfg2.html'
            Text = 'Write and verify an envelope whose payload was already compressed'
        },
        @{
            Page = 'api\StateForge.Security.StateForgeAesKeyRingManager.html'
            Text = 'Create, save, and later rotate a key ring'
        },
        @{
            Page = 'api\StateForge.Prometheus.StateForgePrometheusCollector.html'
            Text = 'Return the metrics from an ASP.NET Core endpoint'
        },
        @{
            Page = 'api\StateForge.Performance.StateForgeStoreSnapshotCache.html'
            Text = 'Persist a snapshot for a monitoring sidecar'
        },
        @{
            Page = 'api\StateForge.Replication.StateForgeFileReplicator.html'
            Text = 'Replicate the current records to one named replica'
        },
        @{
            Page = 'api\StateForge.Snapshots.StateForgeSnapshotService.html'
            Text = 'Create a named snapshot and check its result'
        },
        @{
            Page = 'api\StateForge.Telemetry.StateForgeMetrics.html'
            Text = 'Capture counters for a custom health or metrics endpoint'
        }
    )

    foreach ($requiredExample in $requiredExamples) {
        $examplePagePath = Join-Path -Path $siteRoot -ChildPath $requiredExample.Page
        if (-not (Test-Path -LiteralPath $examplePagePath)) {
            throw "Generated API example page is missing: $examplePagePath"
        }

        $examplePageText = Get-Content -LiteralPath $examplePagePath -Raw
        if ($examplePageText -notmatch '<h[24][^>]+examples[^>]*>Examples</h[24]>' -or
            $examplePageText -notmatch [regex]::Escape($requiredExample.Text)) {
            throw "Generated API example is missing from $($requiredExample.Page): $($requiredExample.Text)"
        }
    }

    $brokenLinks = New-Object System.Collections.Generic.List[string]
    $sitePath = (Resolve-Path -LiteralPath $siteRoot).Path

    Get-ChildItem -LiteralPath $sitePath -Filter '*.html' -Recurse | ForEach-Object {
        $page = $_
        $pageText = Get-Content -LiteralPath $page.FullName -Raw

        foreach ($linkMatch in [regex]::Matches($pageText, 'href="(?<href>[^"]+)"')) {
            $href = $linkMatch.Groups['href'].Value
            if ([string]::IsNullOrWhiteSpace($href) -or
                $href -match '^(https?:|mailto:|#|javascript:|data:)') {
                continue
            }

            $targetText = ($href -split '[?#]')[0]
            if ([string]::IsNullOrWhiteSpace($targetText)) {
                continue
            }

            $decodedTarget = [uri]::UnescapeDataString($targetText).Replace(
                '/',
                [System.IO.Path]::DirectorySeparatorChar)
            $targetPath = [System.IO.Path]::GetFullPath(
                (Join-Path -Path $page.DirectoryName -ChildPath $decodedTarget))

            if (-not (Test-Path -LiteralPath $targetPath)) {
                $relativePage = $page.FullName.Substring($sitePath.Length + 1)
                $brokenLinks.Add($relativePage + ' -> ' + $href)
                continue
            }

            if ($href.Contains('#') -and
                [string]::Equals(
                    [System.IO.Path]::GetExtension($targetPath),
                    '.html',
                    [System.StringComparison]::OrdinalIgnoreCase)) {
                $fragment = [uri]::UnescapeDataString(($href -split '#', 2)[1])
                if (-not [string]::IsNullOrWhiteSpace($fragment)) {
                    $targetPageText = Get-Content -LiteralPath $targetPath -Raw
                    $fragmentPattern = '(id|name)="' + [regex]::Escape($fragment) + '"'
                    if ($targetPageText -notmatch $fragmentPattern) {
                        $relativePage = $page.FullName.Substring($sitePath.Length + 1)
                        $brokenLinks.Add($relativePage + ' -> ' + $href + ' (missing fragment)')
                    }
                }
            }
        }
    }

    if ($brokenLinks.Count -gt 0) {
        $uniqueBrokenLinks = $brokenLinks | Sort-Object -Unique
        throw "Generated documentation contains broken internal links: $($uniqueBrokenLinks -join '; ')"
    }

    [PSCustomObject]@{
        PackageNamespaces = $requiredNamespaces.Count
        EnforcedProjects  = $documentedProjects.Count
        CuratedExamples   = $requiredExamples.Count
        BrokenLinks       = 0
        Success           = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
