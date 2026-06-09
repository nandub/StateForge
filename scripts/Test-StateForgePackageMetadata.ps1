<#
.SYNOPSIS
Validates package metadata for StateForge package projects.

.DESCRIPTION
Checks package-oriented projects for NuGet README, license, project URL, repository URL, tags, and README-NUGET.md inclusion.

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
        'src\StateForge.Prometheus\StateForge.Prometheus.csproj'
    )

    $errors = New-Object System.Collections.Generic.List[string]

    foreach ($project in $projects) {
        $path = Join-Path -Path $repoRoot -ChildPath $project
        $content = Get-Content -LiteralPath $path -Raw

        foreach ($tag in @('PackageReadmeFile', 'PackageLicenseExpression', 'PackageProjectUrl', 'RepositoryUrl', 'PackageTags')) {
            if ($content -notmatch "<$tag>") {
                $errors.Add("$project missing <$tag>.")
            }
        }

        if ($content -notmatch 'README-NUGET\.md') {
            $errors.Add("$project does not include README-NUGET.md for packing.")
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
