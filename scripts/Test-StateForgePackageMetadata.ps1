<#
.SYNOPSIS
Validates package metadata for StateForge package projects.

.DESCRIPTION
Checks package-oriented projects for NuGet README, license, project URL,
repository URL, tags, README inclusion, and centralized SourceLink settings.

.EXAMPLE
.\scripts\Test-StateForgePackageMetadata.ps1

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

    $projects = @(
        'src\StateForge.Core\StateForge.Core.csproj',
        'src\StateForge.FileStore\StateForge.FileStore.csproj',
        'src\StateForge.AspNet\StateForge.AspNet.csproj',
        'src\StateForge.AspNetCore\StateForge.AspNetCore.csproj',
        'src\StateForge.Security\StateForge.Security.csproj',
        'src\StateForge.Telemetry\StateForge.Telemetry.csproj',
        'src\StateForge.CloudNative\StateForge.CloudNative.csproj',
        'src\StateForge.Format\StateForge.Format.csproj',
        'src\StateForge.Prometheus\StateForge.Prometheus.csproj',
        'src\StateForge.Performance\StateForge.Performance.csproj',
        'src\StateForge.Replication\StateForge.Replication.csproj',
        'src\StateForge.Snapshots\StateForge.Snapshots.csproj',
        'src\StateForge.Remote\StateForge.Remote.csproj'
    )

    $errors = New-Object System.Collections.Generic.List[string]
    $expectedRepositoryUrl = 'https://github.com/nandub/StateForge'

    foreach ($project in $projects) {
        $path = Join-Path -Path $repoRoot -ChildPath $project
        [xml]$document = Get-Content -LiteralPath $path -Raw
        $content = $document.OuterXml

        foreach ($tag in @('PackageReadmeFile', 'PackageLicenseExpression', 'PackageProjectUrl', 'RepositoryUrl', 'PackageTags')) {
            if ($content -notmatch "<$tag>") {
                $errors.Add("$project missing <$tag>.")
            }
        }

        $properties = $document.Project.PropertyGroup
        if (($properties.RepositoryUrl | Where-Object { $_ }) -ne $expectedRepositoryUrl) {
            $errors.Add("$project has an incorrect RepositoryUrl.")
        }

        if (($properties.PackageProjectUrl | Where-Object { $_ }) -ne $expectedRepositoryUrl) {
            $errors.Add("$project has an incorrect PackageProjectUrl.")
        }

        if ($content -notmatch 'README-NUGET\.md') {
            $errors.Add("$project does not include README-NUGET.md for packing.")
        }
    }

    $targetsPath = Join-Path -Path $repoRoot -ChildPath 'Directory.Build.targets'
    if (-not (Test-Path -LiteralPath $targetsPath)) {
        $errors.Add('Directory.Build.targets is missing.')
    }
    else {
        $targetsContent = Get-Content -LiteralPath $targetsPath -Raw
        foreach ($requiredSetting in @(
            'Microsoft.SourceLink.GitHub',
            'PublishRepositoryUrl',
            'EmbedUntrackedSources',
            'Deterministic',
            'DebugType',
            'IncludeSymbols',
            'SymbolPackageFormat'
        )) {
            if ($targetsContent -notmatch [regex]::Escape($requiredSetting)) {
                $errors.Add("Directory.Build.targets is missing $requiredSetting.")
            }
        }
    }

    if (-not (Test-Path -LiteralPath (Join-Path -Path $repoRoot -ChildPath 'README-NUGET.md'))) {
        $errors.Add('README-NUGET.md is missing.')
    }

    [PSCustomObject]@{
        ProjectCount = $projects.Count
        ErrorCount   = $errors.Count
        Success      = ($errors.Count -eq 0)
        Errors       = $errors.ToArray()
    }

    if ($errors.Count -gt 0) {
        throw "Package metadata validation failed."
    }
}
catch {
    Write-Error -ErrorRecord $_
}
