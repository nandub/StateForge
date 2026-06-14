<#
.SYNOPSIS
Validates public API compatibility for all StateForge packages.

.DESCRIPTION
Builds package projects and compares their exported public API against
reviewed text baselines. Use UpdateBaseline only after reviewing an intentional
public API addition or breaking change.

.PARAMETER UpdateBaseline
Rewrites API baseline files from the current assemblies.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [switch]$UpdateBaseline
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    $baselineRoot = Join-Path -Path $repoRoot -ChildPath 'api-baselines'
    $validatorProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.ApiCompatibilityTests\StateForge.ApiCompatibilityTests.csproj'
    $mode = 'verify'
    if ($UpdateBaseline) {
        $mode = 'update'
    }

    $packages = @(
        @{ Name = 'StateForge.Core'; Framework = 'netstandard2.0'; ValidatorFramework = 'net8.0' },
        @{ Name = 'StateForge.FileStore'; Framework = 'netstandard2.0'; ValidatorFramework = 'net8.0' },
        @{ Name = 'StateForge.AspNet'; Framework = 'net481'; ValidatorFramework = 'net481' },
        @{ Name = 'StateForge.AspNetCore'; Framework = 'netstandard2.0'; ValidatorFramework = 'net8.0' },
        @{ Name = 'StateForge.Security'; Framework = 'netstandard2.0'; ValidatorFramework = 'net8.0' },
        @{ Name = 'StateForge.Telemetry'; Framework = 'netstandard2.0'; ValidatorFramework = 'net8.0' },
        @{ Name = 'StateForge.CloudNative'; Framework = 'net8.0'; ValidatorFramework = 'net8.0' },
        @{ Name = 'StateForge.Format'; Framework = 'netstandard2.0'; ValidatorFramework = 'net8.0' },
        @{ Name = 'StateForge.Prometheus'; Framework = 'netstandard2.0'; ValidatorFramework = 'net8.0' },
        @{ Name = 'StateForge.Performance'; Framework = 'netstandard2.0'; ValidatorFramework = 'net8.0' },
        @{ Name = 'StateForge.Replication'; Framework = 'netstandard2.0'; ValidatorFramework = 'net8.0' },
        @{ Name = 'StateForge.Snapshots'; Framework = 'netstandard2.0'; ValidatorFramework = 'net8.0' }
    )

    $expectedBaselines = @($packages | ForEach-Object { $_.Name + '.txt' })
    $existingBaselines = @()
    if (Test-Path -LiteralPath $baselineRoot) {
        $existingBaselines = @(Get-ChildItem -LiteralPath $baselineRoot -Filter '*.txt' -File |
            Select-Object -ExpandProperty Name)
    }

    $unexpectedBaselines = @($existingBaselines | Where-Object { $expectedBaselines -notcontains $_ })
    if ($unexpectedBaselines.Count -gt 0) {
        throw "Unexpected API baseline file(s): $($unexpectedBaselines -join ', ')"
    }

    foreach ($package in $packages) {
        $projectPath = Join-Path -Path $repoRoot -ChildPath ("src\{0}\{0}.csproj" -f $package.Name)
        & dotnet build $projectPath --configuration Release
        if ($LASTEXITCODE -ne 0) {
            throw "Build failed for $($package.Name) with exit code $LASTEXITCODE."
        }

        $assemblyPath = Join-Path -Path $repoRoot -ChildPath (
            "src\{0}\bin\Release\{1}\{0}.dll" -f $package.Name, $package.Framework)
        $baselinePath = Join-Path -Path $baselineRoot -ChildPath ($package.Name + '.txt')

        & dotnet run --project $validatorProject --configuration Release `
            --framework $package.ValidatorFramework -- $assemblyPath $baselinePath $mode
        if ($LASTEXITCODE -ne 0) {
            throw "API compatibility validation failed for $($package.Name)."
        }
    }

    [PSCustomObject]@{
        PackageCount   = $packages.Count
        BaselineRoot   = $baselineRoot
        Updated        = [bool]$UpdateBaseline
        Success        = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
