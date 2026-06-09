<#
.SYNOPSIS
Builds StateForge NuGet package artifacts.

.DESCRIPTION
Runs dotnet pack for package-oriented StateForge projects and writes artifacts to the output directory.

.PARAMETER OutputPath
Directory where .nupkg files are written.

.PARAMETER Configuration
Build configuration.

.PARAMETER Version
Package version.

.EXAMPLE
.\scripts\Build-StateForgePackages.ps1 -OutputPath .\artifacts\nuget -Version 0.15.0

.INPUTS
None.

.OUTPUTS
System.Management.Automation.PSCustomObject.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter()]
    [string]$OutputPath = '.\artifacts\nuget',

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [Parameter()]
    [string]$Version = '0.22.1'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
    $repoRoot = Split-Path -Parent $scriptRoot
    $resolvedOutput = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)

    if (-not (Test-Path -LiteralPath $resolvedOutput)) {
        New-Item -Path $resolvedOutput -ItemType Directory -Force | Out-Null
    }

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
        'src\StateForge.Replication\StateForge.Replication.csproj'
    )

    foreach ($project in $projects) {
        $projectPath = Join-Path -Path $repoRoot -ChildPath $project

        & dotnet pack $projectPath `
            --configuration $Configuration `
            --output $resolvedOutput `
            /p:PackageVersion=$Version `
            /p:IncludeSymbols=false `
            /p:ContinuousIntegrationBuild=true `
            /p:IncludeSymbols=true `
            /p:SymbolPackageFormat=snupkg

        if ($LASTEXITCODE -ne 0) {
            throw "dotnet pack failed for $project with exit code $LASTEXITCODE."
        }
    }

    $packages = Get-ChildItem -LiteralPath $resolvedOutput -Filter '*.nupkg' -File
    $symbolPackages = Get-ChildItem -LiteralPath $resolvedOutput -Filter '*.snupkg' -File

    [PSCustomObject]@{
        OutputPath   = $resolvedOutput
        PackageCount = $packages.Count
        SymbolPackageCount = $symbolPackages.Count
        Success      = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}
