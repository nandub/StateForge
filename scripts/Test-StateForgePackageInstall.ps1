<#
.SYNOPSIS
Tests installation of built StateForge packages from an isolated local feed.

.DESCRIPTION
Creates temporary net8.0 and net481 consumer projects, restores all StateForge
packages from the supplied local feed, and builds both projects.

.PARAMETER PackagePath
Directory containing package artifacts.

.PARAMETER Version
Expected package version.

.NOTES
Compatible with Windows PowerShell 5.1.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [Parameter()]
    [string]$Version = '1.0.0'
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

try {
    $resolvedPackagePath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PackagePath)
    if (-not (Test-Path -LiteralPath $resolvedPackagePath -PathType Container)) {
        throw "Package directory not found: $resolvedPackagePath"
    }

    $tempRoot = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ('StateForgePackageInstall-' + [Guid]::NewGuid().ToString('N'))
    New-Item -Path $tempRoot -ItemType Directory -Force | Out-Null

    try {
        $escapedFeed = [System.Security.SecurityElement]::Escape($resolvedPackagePath)
        $nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="stateforge-local" value="$escapedFeed" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
"@
        Set-Content -LiteralPath (Join-Path $tempRoot 'NuGet.config') -Value $nugetConfig -Encoding UTF8

        $net8Project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="StateForge.Core" Version="$Version" />
    <PackageReference Include="StateForge.FileStore" Version="$Version" />
    <PackageReference Include="StateForge.AspNetCore" Version="$Version" />
    <PackageReference Include="StateForge.Security" Version="$Version" />
    <PackageReference Include="StateForge.Telemetry" Version="$Version" />
    <PackageReference Include="StateForge.CloudNative" Version="$Version" />
    <PackageReference Include="StateForge.Format" Version="$Version" />
    <PackageReference Include="StateForge.Prometheus" Version="$Version" />
    <PackageReference Include="StateForge.Performance" Version="$Version" />
    <PackageReference Include="StateForge.Replication" Version="$Version" />
    <PackageReference Include="StateForge.Snapshots" Version="$Version" />
  </ItemGroup>
</Project>
"@
        $net8Root = Join-Path $tempRoot 'net8'
        New-Item -Path $net8Root -ItemType Directory -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $net8Root 'PackageConsumer.csproj') -Value $net8Project -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $net8Root 'Consumer.cs') -Value 'public sealed class Consumer { }' -Encoding UTF8

        $net481Project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net481</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="StateForge.AspNet" Version="$Version" />
  </ItemGroup>
</Project>
"@
        $net481Root = Join-Path $tempRoot 'net481'
        New-Item -Path $net481Root -ItemType Directory -Force | Out-Null
        Set-Content -LiteralPath (Join-Path $net481Root 'PackageConsumer.csproj') -Value $net481Project -Encoding UTF8
        Set-Content -LiteralPath (Join-Path $net481Root 'Consumer.cs') -Value 'public sealed class Consumer { }' -Encoding UTF8

        foreach ($project in @(
            (Join-Path $net8Root 'PackageConsumer.csproj'),
            (Join-Path $net481Root 'PackageConsumer.csproj')
        )) {
            & dotnet restore $project --configfile (Join-Path $tempRoot 'NuGet.config')
            if ($LASTEXITCODE -ne 0) {
                throw "Package consumer restore failed for $project with exit code $LASTEXITCODE."
            }

            & dotnet build $project --configuration Release --no-restore
            if ($LASTEXITCODE -ne 0) {
                throw "Package consumer build failed for $project with exit code $LASTEXITCODE."
            }
        }

        [PSCustomObject]@{
            PackagePath = $resolvedPackagePath
            Version     = $Version
            ProjectCount = 2
            Success     = $true
        }
    }
    finally {
        if (Test-Path -LiteralPath $tempRoot) {
            Remove-Item -LiteralPath $tempRoot -Recurse -Force
        }
    }
}
catch {
    Write-Error -ErrorRecord $_
}
