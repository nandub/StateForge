<#
.SYNOPSIS
Validates the expected StateForge repository layout.

.DESCRIPTION
Checks that the required StateForge source, test, script, and documentation files exist.
This script intentionally uses an explicit file list so accidental deletions are caught early.

.EXAMPLE
.\scripts\Test-StateForgeLayout.ps1

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

    $requiredFiles = @(
        'README.md',
        'README-NUGET.md',
        'CHANGELOG.md',
        'NuGet.config',
        'config\stateforge-maintenance.sample.json',
        'StateForge.sln',

        'src\StateForge.Core\StateForge.Core.csproj',
        'src\StateForge.Format\StateForge.Format.csproj',
        'src\StateForge.PrometheusTests\StateForge.PrometheusTests.csproj',
        'src\StateForge.Prometheus\StateForge.Prometheus.csproj',
        'src\StateForge.FormatTests\StateForge.FormatTests.csproj',
        'src\StateForge.FormatHarness\StateForge.FormatHarness.csproj',
        'src\StateForge.MigrationHarness\StateForge.MigrationHarness.csproj',
        'src\StateForge.StoreMigrationHarness\StateForge.StoreMigrationHarness.csproj',
        'src\StateForge.FileStore\StateForge.FileStore.csproj',
        'src\StateForge.AspNet\StateForge.AspNet.csproj',
        'src\StateForge.AspNetCore\StateForge.AspNetCore.csproj',
        'src\StateForge.Maintenance\StateForge.Maintenance.csproj',
        'src\StateForge.Maintenance.Host\StateForge.Maintenance.Host.csproj',
        'src\StateForge.Security\StateForge.Security.csproj',
        'src\StateForge.Telemetry\StateForge.Telemetry.csproj',
        'src\StateForge.Telemetry.AspNetCore\StateForge.Telemetry.AspNetCore.csproj',
        'src\StateForge.Tools\StateForge.Tools.csproj',
        'src\StateForge.SmokeTests\StateForge.SmokeTests.csproj',
        'src\StateForge.Benchmarks\StateForge.Benchmarks.csproj',
        'src\StateForge.FarmTests\StateForge.FarmTests.csproj',
        'src\StateForge.ResilienceTests\StateForge.ResilienceTests.csproj',
        'src\StateForge.AspNetHarness\StateForge.AspNetHarness.csproj',
        'src\StateForge.KestrelHarness\StateForge.KestrelHarness.csproj',
        'src\StateForge.KestrelClientTest\StateForge.KestrelClientTest.csproj',
        'tests\StateForge.FileStore.Tests\StateForge.FileStore.Tests.csproj',

        'scripts\Build-StateForge.ps1',
        'scripts\Test-StateForgeRelease.ps1',
        'scripts\Test-StateForgeObservability.ps1',
        'scripts\Test-StateForgePrometheus.ps1',
        'scripts\Test-StateForgeDashboard.ps1',
        'scripts\Build-StateForgePackages.ps1',
        'scripts\Test-StateForgePackageMetadata.ps1',
        'scripts\Repair-StateForgeSolution.ps1',
        'scripts\Test-StateForgeLayout.ps1',
        'scripts\Test-StateForgeSolution.ps1',
        'scripts\Test-StateForgeSource.ps1',
        'scripts\Test-NuGetSources.ps1',
        'scripts\Test-StateForge.ps1',
        'scripts\Invoke-StateForgeSmokeTest.ps1',
        'scripts\Invoke-StateForgeBenchmark.ps1',
        'scripts\Invoke-StateForgeFarmTest.ps1',
        'scripts\Invoke-StateForgeResilienceTest.ps1',
        'scripts\Invoke-StateForgeAspNetHarness.ps1',
        'scripts\Start-StateForgeKestrelHarness.ps1',
        'scripts\Test-StateForgeKestrelHarness.ps1',
        'scripts\Test-StateForgeTelemetry.ps1',
        'scripts\New-StateForgeKeyRing.ps1',
        'scripts\Invoke-StateForgeMaintenance.ps1',
        'scripts\Unregister-StateForgeMaintenanceTask.ps1',
        'scripts\Register-StateForgeMaintenanceTask.ps1',
        'scripts\Test-StateForgeMaintenanceHost.ps1',
        'scripts\Test-StateForgeMaintenanceTask.ps1',
        'scripts\Test-StateForgeMaintenanceConfig.ps1',
        'scripts\Invoke-StateForgeMaintenanceHost.ps1',
        'scripts\Test-StateForgeKeyRing.ps1',
        'scripts\Test-StateForgeFormat.ps1',
        'scripts\Test-StateForgeStfg2Envelope.ps1',
        'scripts\Test-StateForgeStfg2Migration.ps1',
        'scripts\Test-StateForgeStfg2StoreMigration.ps1',
        'scripts\Rotate-StateForgeKeyRing.ps1',
        'scripts\Test-StateForgeHealth.ps1',
        'scripts\Show-StateForgeSmokeDemo.ps1',

        'docs\getting-started.md',
        'docs\architecture.md',
        'docs\configuration.md',
        'docs\aspnet-provider.md',
        'docs\aspnetcore-provider.md',
        'docs\kestrel-harness.md',
        'docs\encryption.md',
        'docs\farm-mode.md',
        'docs\cli-reference.md',
        'docs\testing.md',
        'docs\benchmarking.md',
        'docs\troubleshooting.md',
        'docs\telemetry.md',
        'docs\key-rotation.md',
        'docs\stfg2-format.md',
        'docs\stfg2-envelope.md',
        'docs\stfg2-migration.md',
        'docs\stfg2-store-migration.md',
        'docs\maintenance.md',
        'docs\maintenance-host.md',
        'docs\release-packaging.md',
        'docs\nuget-packaging.md',
        'docs\observability.md',
        'docs\grafana-dashboard.md',
        'docs\prometheus.md',
        'docs\production-deployment.md'
    )

    $missing = New-Object System.Collections.Generic.List[string]

    foreach ($relativePath in $requiredFiles) {
        $fullPath = Join-Path -Path $repoRoot -ChildPath $relativePath

        if (-not (Test-Path -LiteralPath $fullPath)) {
            $missing.Add($relativePath)
        }
    }

    [PSCustomObject]@{
        RepositoryRoot = $repoRoot
        RequiredFiles  = $requiredFiles.Count
        MissingFiles   = $missing.Count
        Success        = ($missing.Count -eq 0)
    }

    if ($missing.Count -gt 0) {
        Write-Error ("Missing required file(s): {0}" -f ($missing -join ', '))
    }
}
catch {
    Write-Error -ErrorRecord $_
}
