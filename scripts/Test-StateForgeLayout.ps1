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
        'src\StateForge.ReplicaMonitoringTests\StateForge.ReplicaMonitoringTests.csproj',
        'src\StateForge.ScaleTests\StateForge.ScaleTests.csproj',
        'src\StateForge.PerformanceTests\StateForge.PerformanceTests.csproj',
        'src\StateForge.SnapshotTests\StateForge.SnapshotTests.csproj',
        'src\StateForge.Performance\StateForge.Performance.csproj',
        'src\StateForge.ApiValidationTests\StateForge.ApiValidationTests.csproj',
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
        'scripts\Test-StateForgeLargeScale.ps1',
        'scripts\Compare-StateForgeBenchmark.ps1',
        'scripts\Test-StateForgeScale.ps1',
        'scripts\Test-StateForgeSharding.ps1',
        'scripts\Test-StateForgePerformance.ps1',
        'scripts\New-StateForgeSnapshot.ps1',
        'scripts\Test-StateForgeSnapshotMetrics.ps1',
        'scripts\Update-StateForgeStoreSnapshot.ps1',
        'scripts\Test-StateForgeApiValidation.ps1',
        'scripts\Invoke-StateForgeScaleTest.ps1',
        'scripts\Test-StateForgePrometheus.ps1',
        'scripts\Test-StateForgeReplicaMonitoring.ps1',
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
    'docs\README.md',
    'docs\01-getting-started.md',
    'docs\02-architecture.md',
    'docs\03-disaster-recovery.md',
    'docs\04-observability.md',
    'docs\05-testing.md',
    'docs\06-solution-layout.md',
    'docs\07-roadmap.md',
    'docs\08-api-reference.md',
    'docs\09-release-history.md',
    'docs\10-contributing.md'
    'scripts\Invoke-StateForge.ps1'
    'docs\11-script-reference.md'
    'docs\12-production-readiness.md',
    'docs\13-runbooks.md',
    'scripts\Test-StateForgeProduction.ps1'
    'scripts\Test-StateForgeProductionNonInteractive.ps1'
    'src\StateForge.ReplicaCatchUpTests\StateForge.ReplicaCatchUpTests.csproj',
    'scripts\Test-StateForgeReplicaCatchUp.ps1',
    'docs\14-replica-catch-up.md'
    'AGENTS.md'


































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

# v0.30.0: scripts\Test-StateForgeVersionConsistency.ps1
