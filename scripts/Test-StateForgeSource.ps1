<#
.SYNOPSIS
Validates critical StateForge source patterns.

.DESCRIPTION
Checks for source-level regressions that are not covered by simple layout validation.

.EXAMPLE
.\scripts\Test-StateForgeSource.ps1

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
    $storePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.FileStore\StateForgeFileStore.cs'

    if (-not (Test-Path -LiteralPath $storePath)) {
        throw "StateForgeFileStore.cs not found: $storePath"
    }

    $source = Get-Content -LiteralPath $storePath -Raw

    if ($source -notmatch 'private\s+StateForgeProtectionMode\s+ResolveProtectionMode\s*\(') {
        throw "Missing method declaration: private StateForgeProtectionMode ResolveProtectionMode()"
    }

    if ($source -notmatch 'ResolveProtectionMode\s*\(\s*\)') {
        throw "Missing ResolveProtectionMode() usage."
    }

    if ($source -notmatch 'public\s+StateForgeHealthResult\s+CheckHealth\s*\(') {
        throw "Missing method declaration: public StateForgeHealthResult CheckHealth()"
    }

    if ($source -notmatch 'public\s+StateForgeValidationResult\s+ValidateConfiguration\s*\(') {
        throw "Missing method declaration: public StateForgeValidationResult ValidateConfiguration()"
    }

    if ($source -notmatch 'private\s+static\s+bool\s+CanWriteDirectory\s*\(') {
        throw "Missing method declaration: private static bool CanWriteDirectory()"
    }

    [PSCustomObject]@{
        SourceFile = $storePath
        Success    = $true
    }
}
catch {
    Write-Error -ErrorRecord $_
}


# Smoke-test demo store should include AES records.
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptRoot
$smokePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.SmokeTests\Program.cs'
$smokeSource = Get-Content -LiteralPath $smokePath -Raw

if ($smokeSource -notmatch 'demo-aes') {
    throw "Missing demo store AES record: demo-aes"
}

if ($smokeSource -notmatch 'demo-compressed-aes') {
    throw "Missing demo store AES record: demo-compressed-aes"
}


# Validate StateForge.Tools helper methods.
$toolsPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Tools\Program.cs'

if (-not (Test-Path -LiteralPath $toolsPath)) {
    throw "Program.cs not found: $toolsPath"
}

$toolsSource = Get-Content -LiteralPath $toolsPath -Raw

if ($toolsSource -notmatch 'private\s+static\s+string\s+StringArrayJson\s*\(') {
    throw "Missing method declaration: private static string StringArrayJson()"
}

# Validate StateForge.SmokeTests helper methods.
$smokePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.SmokeTests\Program.cs'

if (-not (Test-Path -LiteralPath $smokePath)) {
    throw "SmokeTests Program.cs not found: $smokePath"
}

$smokeSource = Get-Content -LiteralPath $smokePath -Raw

if ($smokeSource -notmatch 'private\s+static\s+StateForgeFileStore\s+CreateAesStore\s*\(') {
    throw "Missing method declaration: private static StateForgeFileStore CreateAesStore()"
}

if ($smokeSource -notmatch 'demo-aes') {
    throw "Missing demo store AES record: demo-aes"
}

if ($smokeSource -notmatch 'demo-compressed-aes') {
    throw "Missing demo store AES record: demo-compressed-aes"
}


# Validate StateForge.Tools AES-aware options.
$toolsPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Tools\Program.cs'
$toolsSource = Get-Content -LiteralPath $toolsPath -Raw

if ($toolsSource -notmatch 'CreateOptions\s*\(\s*string\s+root') {
    throw "Missing StateForge.Tools CreateOptions() helper."
}

if ($toolsSource -notmatch '--protection') {
    throw "Missing StateForge.Tools --protection support."
}

if ($toolsSource -notmatch '--aes-key') {
    throw "Missing StateForge.Tools --aes-key support."
}


# Validate smoke demo AES-aware command output.
$smokePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.SmokeTests\Program.cs'
$smokeSource = Get-Content -LiteralPath $smokePath -Raw

if ($smokeSource -notmatch '--protection aes') {
    throw "Smoke-test output does not include AES-aware inspection command."
}

if ($smokeSource -notmatch 'AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=') {
    throw "Smoke-test output does not include deterministic AES demo key."
}


# Validate ASP.NET provider supports keepBackups.
$providerPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.AspNet\StateForgeSessionStateProvider.cs'
$providerSource = Get-Content -LiteralPath $providerPath -Raw

if ($providerSource -notmatch 'keepBackups') {
    throw "ASP.NET provider does not support keepBackups."
}

# Validate solution repair script exists.
$repairScript = Join-Path -Path $repoRoot -ChildPath 'scripts\Repair-StateForgeSolution.ps1'

if (-not (Test-Path -LiteralPath $repairScript)) {
    throw "Missing Repair-StateForgeSolution.ps1"
}


# Validate Kestrel harness source patterns.
$kestrelHarnessPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.KestrelHarness\Program.cs'
$kestrelClientPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.KestrelClientTest\Program.cs'

if (Test-Path -LiteralPath $kestrelHarnessPath) {
    $kestrelHarnessSource = Get-Content -LiteralPath $kestrelHarnessPath -Raw

    if ($kestrelHarnessSource -match 'Path\.' -and $kestrelHarnessSource -notmatch 'using\s+System\.IO;') {
        throw "StateForge.KestrelHarness uses Path but lacks using System.IO;"
    }

    if ($kestrelHarnessSource -match 'Console\.' -and $kestrelHarnessSource -notmatch 'using\s+System;') {
        throw "StateForge.KestrelHarness uses Console but lacks using System;"
    }

    if ($kestrelHarnessSource -match '\w+\?') {
        throw "StateForge.KestrelHarness contains nullable annotation syntax while Nullable is disabled."
    }
}

if (Test-Path -LiteralPath $kestrelClientPath) {
    $kestrelClientSource = Get-Content -LiteralPath $kestrelClientPath -Raw

    if ($kestrelClientSource -match 'Uri\(' -and $kestrelClientSource -notmatch 'using\s+System;') {
        throw "StateForge.KestrelClientTest uses Uri but lacks using System;"
    }

    if ($kestrelClientSource -match 'Console\.' -and $kestrelClientSource -notmatch 'using\s+System;') {
        throw "StateForge.KestrelClientTest uses Console but lacks using System;"
    }

    if ($kestrelClientSource -match '\w+\?') {
        throw "StateForge.KestrelClientTest contains nullable annotation syntax while Nullable is disabled."
    }
}


# Validate ASP.NET harness uses synthetic HttpContext instead of null.
$aspNetHarnessPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.AspNetHarness\Program.cs'

if (Test-Path -LiteralPath $aspNetHarnessPath) {
    $aspNetHarnessSource = Get-Content -LiteralPath $aspNetHarnessPath -Raw

    if ($aspNetHarnessSource -notmatch 'CreateContext\s*\(') {
        throw "ASP.NET harness does not create a synthetic HttpContext."
    }

    if ($aspNetHarnessSource -match 'GetItem\s*\(\s*null') {
        throw "ASP.NET harness still passes null context into provider calls."
    }
}

# Validate harness scripts resolve root paths before passing to dotnet run.
$kestrelStartScript = Join-Path -Path $repoRoot -ChildPath 'scripts\Start-StateForgeKestrelHarness.ps1'
$aspNetHarnessScript = Join-Path -Path $repoRoot -ChildPath 'scripts\Invoke-StateForgeAspNetHarness.ps1'

if ((Test-Path -LiteralPath $kestrelStartScript) -and ((Get-Content -LiteralPath $kestrelStartScript -Raw) -notmatch 'GetUnresolvedProviderPathFromPSPath')) {
    throw "Start-StateForgeKestrelHarness.ps1 does not resolve RootPath before passing it to dotnet run."
}

if ((Test-Path -LiteralPath $aspNetHarnessScript) -and ((Get-Content -LiteralPath $aspNetHarnessScript -Raw) -notmatch 'GetUnresolvedProviderPathFromPSPath')) {
    throw "Invoke-StateForgeAspNetHarness.ps1 does not resolve RootPath before passing it to dotnet run."
}

# Validate StateForge.Tools imports System.IO when File APIs are used.
$toolsProgramPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Tools\Program.cs'

if (Test-Path -LiteralPath $toolsProgramPath) {
    $toolsProgramSource = Get-Content -LiteralPath $toolsProgramPath -Raw

    if ($toolsProgramSource -match '\bFile\.' -and $toolsProgramSource -notmatch 'using\s+System\.IO;') {
        throw "StateForge.Tools uses File APIs but lacks using System.IO;"
    }
}

# Validate migration harness writes deterministic byte payloads without UTF-8 BOM.
$migrationHarnessPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.MigrationHarness\Program.cs'

if (Test-Path -LiteralPath $migrationHarnessPath) {
    $migrationHarnessSource = Get-Content -LiteralPath $migrationHarnessPath -Raw

    if ($migrationHarnessSource -match 'File\.WriteAllText\(.+Encoding\.UTF8') {
        throw "StateForge.MigrationHarness must use File.WriteAllBytes with Encoding.UTF8.GetBytes to avoid BOM-sensitive payload tests."
    }
}

# Validate StateForge.ScaleTests uses the actual FileStore API shape.
$scaleTestPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.ScaleTests\Program.cs'

if (Test-Path -LiteralPath $scaleTestPath) {
    $scaleTestSource = Get-Content -LiteralPath $scaleTestPath -Raw

    if ($scaleTestSource -match 'DateTimeOffset\.UtcNow\.AddHours') {
        throw "StateForge.ScaleTests must call store.Set with TimeSpan, not DateTimeOffset."
    }

    if ($scaleTestSource -match 'byte\[\]\s+value\s*=\s*store\.Get') {
        throw "StateForge.ScaleTests must treat store.Get as returning StateForgeEntry, not byte[]."
    }

    if ($scaleTestSource -notmatch 'StateForgeEntry\s+entry\s*=\s*store\.Get') {
        throw "StateForge.ScaleTests must validate StateForgeEntry returned by store.Get."
    }
}

# Validate StateForgeEntry byte payload access is not hard-coded to Payload.
$scaleTestPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.ScaleTests\Program.cs'
$apiValidationPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.ApiValidationTests\Program.cs'

foreach ($path in @($scaleTestPath, $apiValidationPath)) {
    if (Test-Path -LiteralPath $path) {
        $content = Get-Content -LiteralPath $path -Raw

        if ($content -match '\.Payload') {
            throw "StateForgeEntry does not expose Payload; use the actual byte[] property or reflection-safe payload access."
        }
    }
}


# Validate v0.21 sharding harnesses do not use internal APIs and required references exist.
$prometheusProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Prometheus\StateForge.Prometheus.csproj'
if (Test-Path -LiteralPath $prometheusProject) {
    $prometheusProjectSource = Get-Content -LiteralPath $prometheusProject -Raw

    if ($prometheusProjectSource -notmatch 'StateForge\.Performance') {
        throw "StateForge.Prometheus must reference StateForge.Performance when snapshot Prometheus files are present."
    }
}

$shardingTestPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.ShardingTests\Program.cs'
if (Test-Path -LiteralPath $shardingTestPath) {
    $shardingTestSource = Get-Content -LiteralPath $shardingTestPath -Raw

    if ($shardingTestSource -match 'SafeKey\.Hash') {
        throw "StateForge.ShardingTests must not call internal SafeKey.Hash."
    }
}

$shardingMigrationPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.ShardingMigrationHarness\Program.cs'
if (Test-Path -LiteralPath $shardingMigrationPath) {
    $shardingMigrationSource = Get-Content -LiteralPath $shardingMigrationPath -Raw

    if ($shardingMigrationSource -notmatch 'using StateForge\.Core;') {
        throw "StateForge.ShardingMigrationHarness must import StateForge.Core for StateForgeEntry."
    }
}

# Validate v0.21 replication foundations exist.
$replicationProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForge.Replication.csproj'
if (-not (Test-Path -LiteralPath $replicationProject)) {
    throw "StateForge.Replication project is required for v0.21.0."
}

$replicationTestProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.ReplicationTests\StateForge.ReplicationTests.csproj'
if (-not (Test-Path -LiteralPath $replicationTestProject)) {
    throw "StateForge.ReplicationTests project is required for v0.21.0."
}

# Validate v0.22 replication services exist.
$replicationHostProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication.Host\StateForge.Replication.Host.csproj'
if (-not (Test-Path -LiteralPath $replicationHostProject)) {
    throw "StateForge.Replication.Host project is required for v0.22.0."
}

$replicationServiceScript = Join-Path -Path $repoRoot -ChildPath 'scripts\Test-StateForgeReplicationService.ps1'
if (-not (Test-Path -LiteralPath $replicationServiceScript)) {
    throw "Test-StateForgeReplicationService.ps1 is required for v0.22.0."
}

# Validate v0.22.1 manifest writer does not contain corrupted escaped C# strings.
$replicatorPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeFileReplicator.cs'

if (Test-Path -LiteralPath $replicatorPath) {
    $replicatorSource = Get-Content -LiteralPath $replicatorPath -Raw

    if ($replicatorSource -match '\\\\"version\\\\"') {
        throw "StateForgeFileReplicator contains corrupted over-escaped JSON strings."
    }

    if ($replicatorSource -notmatch 'WriteManifest') {
        throw "StateForgeFileReplicator must include WriteManifest."
    }

    if ($replicatorSource -notmatch 'Replace\("\\\\", "\\\\\\\\"\)') {
        throw "StateForgeFileReplicator Escape method must escape backslashes."
    }
}

# Validate v0.23-v0.26 snapshot/promotion/failover projects exist.
$snapshotsProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Snapshots\StateForge.Snapshots.csproj'
if (-not (Test-Path -LiteralPath $snapshotsProject)) {
    throw "StateForge.Snapshots project is required for v0.23.0 through v0.26.0."
}

$snapshotTests = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.SnapshotServiceTests\StateForge.SnapshotServiceTests.csproj'
if (-not (Test-Path -LiteralPath $snapshotTests)) {
    throw "StateForge.SnapshotServiceTests project is required for v0.23.0 through v0.26.0."
}

# Validate v0.26.1 snapshot marker writers do not contain multiline C# string constants.
$promotionServicePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Snapshots\StateForgeReplicaPromotionService.cs'
$failoverServicePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Snapshots\StateForgeFailoverService.cs'

foreach ($servicePath in @($promotionServicePath, $failoverServicePath)) {
    if (Test-Path -LiteralPath $servicePath) {
        $serviceSource = Get-Content -LiteralPath $servicePath -Raw

        if ($serviceSource -match 'string\s+marker\s*=\s*"\{') {
            throw "Snapshot marker writers must not use raw multiline C# string constants."
        }

        if ($serviceSource -notmatch 'StringBuilder') {
            throw "Snapshot marker writers must use StringBuilder for JSON marker output."
        }
    }
}

$manualJsonFiles = @(
    'src\StateForge.Replication\StateForgeFileReplicator.cs',
    'src\StateForge.Snapshots\StateForgeSnapshotService.cs',
    'src\StateForge.Snapshots\StateForgeReplicaPromotionService.cs',
    'src\StateForge.Snapshots\StateForgeFailoverService.cs'
)

foreach ($manualJsonFile in $manualJsonFiles) {
    $manualJsonPath = Join-Path -Path $repoRoot -ChildPath $manualJsonFile

    if (Test-Path -LiteralPath $manualJsonPath) {
        $manualJsonSource = Get-Content -LiteralPath $manualJsonPath -Raw

        if ($manualJsonSource -match 'string\s+\w+\s*=\s*"\{') {
            throw "Manual JSON file contains raw multiline C# string constant: $manualJsonFile"
        }
    }
}

# Validate v0.27 incremental snapshot components exist.
$incrementalProject = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.IncrementalSnapshotTests\StateForge.IncrementalSnapshotTests.csproj'
if (-not (Test-Path -LiteralPath $incrementalProject)) {
    throw "StateForge.IncrementalSnapshotTests project is required for v0.27.0."
}

$incrementalService = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Snapshots\StateForgeIncrementalSnapshotService.cs'
if (-not (Test-Path -LiteralPath $incrementalService)) {
    throw "StateForgeIncrementalSnapshotService is required for v0.27.0."
}

# Validate v0.27.1 documentation consolidation files exist.
$documentationFiles = @(
    'docs\README.md',




    'scripts\Test-StateForgeDocs.ps1'
)

foreach ($documentationFile in $documentationFiles) {
    $documentationPath = Join-Path -Path $repoRoot -ChildPath $documentationFile

    if (-not (Test-Path -LiteralPath $documentationPath)) {
        throw "Missing v0.27.1 documentation consolidation file: $documentationFile"
    }
}

# Validate v0.28.x consolidated documentation model.
$consolidatedDocs = @(
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
    'docs\10-contributing.md',
    'docs\11-script-reference.md'
)

foreach ($consolidatedDoc in $consolidatedDocs) {
    $consolidatedPath = Join-Path -Path $repoRoot -ChildPath $consolidatedDoc

    if (-not (Test-Path -LiteralPath $consolidatedPath)) {
        throw "Missing consolidated documentation file: $consolidatedDoc"
    }
}


# Validate v0.30.3 docs cleanup: build script must not require legacy documentation files.
$buildScriptPath = Join-Path -Path $repoRoot -ChildPath 'scripts\Build-StateForge.ps1'
if (Test-Path -LiteralPath $buildScriptPath) {
    $buildScriptText = Get-Content -LiteralPath $buildScriptPath -Raw
    $legacyDocs = @(
        'docs\getting-started.md',
        'docs\architecture.md',
        'docs\configuration.md',
        'docs\aspnet-provider.md',
        'docs\aspnetcore-provider.md',
        'docs\testing.md',
        'docs\telemetry.md',
        'docs\prometheus.md',
        'docs\production-deployment.md'
    )

    foreach ($legacyDoc in $legacyDocs) {
        if ($buildScriptText -like "*$legacyDoc*") {
            throw "Build-StateForge.ps1 must not require legacy documentation files: $legacyDoc"
        }
    }
}


# v0.30.3: Documentation shape is validated by Test-StateForgeDocs.ps1.
# Test-StateForgeSource.ps1 validates source structure only.


# Validate v0.30.3 operational dispatcher scope.
$invokeStateForgePath = Join-Path -Path $repoRoot -ChildPath 'scripts\Invoke-StateForge.ps1'
if (Test-Path -LiteralPath $invokeStateForgePath) {
    $invokeStateForgeText = Get-Content -LiteralPath $invokeStateForgePath -Raw
    $parameterHeavyCommands = @(
        'RunMaintenanceHost',
        'StartReplicationHost',
        'NewIncrementalSnapshot',
        'NewSnapshot',
        'RotateKeyRing',
        'RegisterMaintenanceTask',
        'UnregisterMaintenanceTask'
    )

    foreach ($parameterHeavyCommand in $parameterHeavyCommands) {
        if ($invokeStateForgeText -match "'$parameterHeavyCommand'") {
            throw "Invoke-StateForge.ps1 must not expose parameter-heavy operational commands: $parameterHeavyCommand"
        }
    }
}


# Validate v0.30.3 production-readiness files.
$productionReadinessFiles = @(
    'docs\12-production-readiness.md',
    'docs\13-runbooks.md',
    'scripts\Test-StateForgeProduction.ps1'
)

foreach ($productionReadinessFile in $productionReadinessFiles) {
    $productionReadinessPath = Join-Path -Path $repoRoot -ChildPath $productionReadinessFile

    if (-not (Test-Path -LiteralPath $productionReadinessPath)) {
        throw "Missing production-readiness file: $productionReadinessFile"
    }
}

$testRunnerPath = Join-Path -Path $repoRoot -ChildPath 'scripts\Test-StateForge.ps1'
$testRunnerText = Get-Content -LiteralPath $testRunnerPath -Raw

if ($testRunnerText -notmatch "'Production'") {
    throw "Test-StateForge.ps1 must expose the Production suite."
}


# Validate v0.30.3 non-interactive production validation.
$productionRunnerPath = Join-Path -Path $repoRoot -ChildPath 'scripts\Test-StateForge.ps1'
$productionRunnerText = Get-Content -LiteralPath $productionRunnerPath -Raw

if ($productionRunnerText -notmatch 'Get-StateForgeProductionHealthRoot') {
    throw 'Production validation must define a default health root.'
}

if ($productionRunnerText -notmatch "Test-StateForgeHealth\.ps1'\s+-Arguments\s+@\{\s*RootPath") {
    throw 'Production validation must provide RootPath to Test-StateForgeHealth.ps1.'
}


# Validate v0.30.3 replica catch-up files.
$replicaCatchUpFiles = @(
    'src\StateForge.Replication\StateForgeReplicaCatchUpService.cs',
    'src\StateForge.Replication\StateForgeReplicaCatchUpOptions.cs',
    'src\StateForge.ReplicaCatchUpTests\StateForge.ReplicaCatchUpTests.csproj',
    'scripts\Test-StateForgeReplicaCatchUp.ps1',
    'docs\14-replica-catch-up.md'
)

foreach ($replicaCatchUpFile in $replicaCatchUpFiles) {
    $replicaCatchUpPath = Join-Path -Path $repoRoot -ChildPath $replicaCatchUpFile

    if (-not (Test-Path -LiteralPath $replicaCatchUpPath)) {
        throw "Missing replica catch-up file: $replicaCatchUpFile"
    }
}

$testRunnerPath = Join-Path -Path $repoRoot -ChildPath 'scripts\Test-StateForge.ps1'
$testRunnerText = Get-Content -LiteralPath $testRunnerPath -Raw

if ($testRunnerText -notmatch "'ReplicaCatchUp'") {
    throw "Test-StateForge.ps1 must expose the ReplicaCatchUp suite."
}


# Validate v0.30.3 replica catch-up hash detection.
$replicaCatchUpServicePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeReplicaCatchUpService.cs'
$replicaCatchUpServiceText = Get-Content -LiteralPath $replicaCatchUpServicePath -Raw

if ($replicaCatchUpServiceText -notmatch 'SHA256') {
    throw 'Replica catch-up changed-file detection must use SHA256 content hashing.'
}

if ($replicaCatchUpServiceText -match 'LastWriteUtc') {
    throw 'Replica catch-up changed-file detection must not rely on LastWriteUtc.'
}


# Validate v0.30.3 deterministic replica catch-up test fixture.
$replicaCatchUpTestPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.ReplicaCatchUpTests\Program.cs'
$replicaCatchUpTestText = Get-Content -LiteralPath $replicaCatchUpTestPath -Raw

if ($replicaCatchUpTestText -match 'StateForgeFileStore') {
    throw 'Replica catch-up tests must use deterministic filesystem fixtures, not FileStore-generated paths.'
}

if ($replicaCatchUpTestText -notmatch 'equal-length changed file detection') {
    throw 'Replica catch-up tests must validate equal-length changed file detection.'
}

# Validate v0.30.4 hardening invariants.
$snapshotPathGuardPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Snapshots\StateForgeSnapshotPath.cs'
$incrementalSnapshotPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Snapshots\StateForgeIncrementalSnapshotService.cs'
$fileStorePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.FileStore\StateForgeFileStore.cs'
$distributedCachePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.AspNetCore\StateForgeDistributedCache.cs'
$promotionPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Snapshots\StateForgeReplicaPromotionService.cs'
$failoverPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Snapshots\StateForgeFailoverService.cs'

if (-not (Test-Path -LiteralPath $snapshotPathGuardPath)) {
    throw 'Missing snapshot path containment helper.'
}

$snapshotPathGuardText = Get-Content -LiteralPath $snapshotPathGuardPath -Raw
$incrementalSnapshotText = Get-Content -LiteralPath $incrementalSnapshotPath -Raw
$fileStoreText = Get-Content -LiteralPath $fileStorePath -Raw
$distributedCacheText = Get-Content -LiteralPath $distributedCachePath -Raw
$promotionText = Get-Content -LiteralPath $promotionPath -Raw
$failoverText = Get-Content -LiteralPath $failoverPath -Raw

if ($snapshotPathGuardText -notmatch 'Path\.IsPathRooted' -or
    $snapshotPathGuardText -notmatch 'StartsWith\(prefix') {
    throw 'Snapshot paths must reject rooted paths and enforce root containment.'
}

if ($incrementalSnapshotText -notmatch 'SHA256' -or
    $incrementalSnapshotText -notmatch 'ResolveRelativePath') {
    throw 'Incremental snapshots must use SHA256 detection and contained manifest paths.'
}

if ($fileStoreText -notmatch '!existing\.Locked\s*\|\|\s*existing\.LockId != lockId') {
    throw 'SetAndUnlock must enforce the current active lock ID as a fencing token.'
}

if ($fileStoreText -notmatch 'entry\.IsExpired\(now\)') {
    throw 'Refresh must reject expired entries.'
}

if ($distributedCacheText -notmatch 'EnvelopeMagic' -or
    $distributedCacheText -notmatch 'AbsoluteExpirationUtc' -or
    $distributedCacheText -notmatch 'SlidingExpiration') {
    throw 'Distributed cache expiration metadata must preserve sliding and absolute deadlines.'
}

if ($promotionText -notmatch 'if \(result\.Success\)' -or
    $failoverText -notmatch 'if \(result\.Success\)') {
    throw 'Recovery markers must only be written after successful operations.'
}

# Validate v0.31.1 replica monitoring stabilization.
$replicaStateStorePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeReplicaStateStore.cs'
$replicaStateMutexPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeReplicaStateMutex.cs'
$replicaConfigurationPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeReplicaConfiguration.cs'
$replicaMonitorPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeReplicaMonitor.cs'
$replicaMetricsPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Prometheus\StateForgeReplicaPrometheusFormatter.cs'
$replicaMonitoringTestPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.ReplicaMonitoringTests\Program.cs'
$replicaMonitoringScriptPath = Join-Path -Path $repoRoot -ChildPath 'scripts\Test-StateForgeReplicaMonitoring.ps1'

foreach ($monitoringPath in @(
    $replicaStateStorePath,
    $replicaStateMutexPath,
    $replicaConfigurationPath,
    $replicaMonitorPath,
    $replicaMetricsPath,
    $replicaMonitoringTestPath,
    $replicaMonitoringScriptPath
)) {
    if (-not (Test-Path -LiteralPath $monitoringPath)) {
        throw "Missing replica monitoring file: $monitoringPath"
    }
}

$replicaStateStoreText = Get-Content -LiteralPath $replicaStateStorePath -Raw
$replicaStateMutexText = Get-Content -LiteralPath $replicaStateMutexPath -Raw
$replicaConfigurationText = Get-Content -LiteralPath $replicaConfigurationPath -Raw
$replicaMonitorText = Get-Content -LiteralPath $replicaMonitorPath -Raw
$replicaMetricsText = Get-Content -LiteralPath $replicaMetricsPath -Raw
$replicaMonitoringTestText = Get-Content -LiteralPath $replicaMonitoringTestPath -Raw

if ($replicaStateStoreText -notmatch 'File\.Replace' -or
    $replicaStateStoreText -notmatch 'InvalidDataException' -or
    $replicaStateStoreText -notmatch 'StateForgeReplicaStateMutex\.Acquire') {
    throw 'Replica monitoring state must be atomic, synchronized, and strictly validated.'
}

if ($replicaStateMutexText -notmatch 'SHA256' -or
    $replicaStateMutexText -notmatch 'WaitOne') {
    throw 'Replica monitoring updates must use a path-scoped named mutex.'
}

if ($replicaConfigurationText -notmatch "IndexOf\('='\)" -or
    $replicaConfigurationText -notmatch 'replica-') {
    throw 'Replica configuration must support named and positional entries.'
}

if ($replicaMonitorText -notmatch 'staleThreshold' -or
    $replicaMonitorText -notmatch 'LagSeconds') {
    throw 'Replica monitoring must calculate lag against a configurable stale threshold.'
}

foreach ($metricName in @(
    'stateforge_replica_lag_seconds',
    'stateforge_replica_healthy',
    'stateforge_replica_last_sync_timestamp',
    'stateforge_replica_catchup_operations_total',
    'stateforge_replica_failed_syncs_total'
)) {
    if ($replicaMetricsText -notmatch [regex]::Escape($metricName)) {
        throw "Missing replica monitoring metric: $metricName"
    }
}

if ($replicaMonitoringTestText -notmatch 'deterministic replica lag calculation' -or
    $replicaMonitoringTestText -notmatch 'concurrent replica state updates' -or
    $replicaMonitoringTestText -notmatch 'corrupt replica state detection') {
    throw 'Replica monitoring tests must cover deterministic lag, concurrency, and corrupt state.'
}

$monitoringRunnerPath = Join-Path -Path $repoRoot -ChildPath 'scripts\Test-StateForge.ps1'
$monitoringRunnerText = Get-Content -LiteralPath $monitoringRunnerPath -Raw

if ($monitoringRunnerText -notmatch "'ReplicaMonitoring'") {
    throw 'Test-StateForge.ps1 must expose the ReplicaMonitoring suite.'
}

# Validate v0.32.0 quorum foundations.
$quorumEvaluatorPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeQuorumEvaluator.cs'
$quorumPolicyPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeQuorumPolicy.cs'
$quorumMemberPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeClusterMember.cs'
$quorumTestPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.QuorumTests\Program.cs'
$quorumScriptPath = Join-Path -Path $repoRoot -ChildPath 'scripts\Test-StateForgeQuorum.ps1'

foreach ($quorumPath in @(
    $quorumEvaluatorPath,
    $quorumPolicyPath,
    $quorumMemberPath,
    $quorumTestPath,
    $quorumScriptPath
)) {
    if (-not (Test-Path -LiteralPath $quorumPath)) {
        throw "Missing quorum foundation file: $quorumPath"
    }
}

$quorumEvaluatorText = Get-Content -LiteralPath $quorumEvaluatorPath -Raw
$quorumTestText = Get-Content -LiteralPath $quorumTestPath -Raw

if ($quorumEvaluatorText -notmatch 'RequiredVotes' -or
    $quorumEvaluatorText -notmatch 'CandidateEligible' -or
    $quorumEvaluatorText -match 'Promote\(') {
    throw 'Quorum foundations must calculate votes and eligibility without automatic promotion.'
}

if ($quorumTestText -notmatch 'majority quorum calculation' -or
    $quorumTestText -notmatch 'no automatic leader election') {
    throw 'Quorum tests must cover majority calculation and the no-election boundary.'
}

if ($monitoringRunnerText -notmatch "'Quorum'") {
    throw 'Test-StateForge.ps1 must expose the Quorum suite.'
}

# Validate v0.33.0 witness nodes.
$witnessStateStorePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeWitnessStateStore.cs'
$witnessEvaluatorPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeWitnessEvaluator.cs'
$witnessNodePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeWitnessNode.cs'
$witnessTestPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.WitnessTests\Program.cs'
$witnessScriptPath = Join-Path -Path $repoRoot -ChildPath 'scripts\Test-StateForgeWitness.ps1'

foreach ($witnessPath in @(
    $witnessStateStorePath,
    $witnessEvaluatorPath,
    $witnessNodePath,
    $witnessTestPath,
    $witnessScriptPath
)) {
    if (-not (Test-Path -LiteralPath $witnessPath)) {
        throw "Missing witness node file: $witnessPath"
    }
}

$witnessStateStoreText = Get-Content -LiteralPath $witnessStateStorePath -Raw
$witnessEvaluatorText = Get-Content -LiteralPath $witnessEvaluatorPath -Raw
$witnessTestText = Get-Content -LiteralPath $witnessTestPath -Raw

if ($witnessStateStoreText -notmatch 'File\.Replace' -or
    $witnessStateStoreText -notmatch 'InvalidDataException') {
    throw 'Witness state must be written atomically and parsed strictly.'
}

if ($witnessEvaluatorText -notmatch 'VoteCounted' -or
    $witnessEvaluatorText -notmatch 'StateForgeClusterMemberRole\.Witness' -or
    $witnessEvaluatorText -match 'Failover') {
    throw 'Witness evaluation must validate votes without automatic failover.'
}

if ($witnessTestText -notmatch 'witness vote restores quorum' -or
    $witnessTestText -notmatch 'no automatic failover integration') {
    throw 'Witness tests must cover quorum integration and the failover boundary.'
}

if ($monitoringRunnerText -notmatch "'Witness'") {
    throw 'Test-StateForge.ps1 must expose the Witness suite.'
}

# Validate v0.34.0 split-brain prevention.
$primaryLeaseStorePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgePrimaryLeaseStore.cs'
$primaryLeaseLockPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgePrimaryLeaseLock.cs'
$promotionFencePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgePromotionFenceService.cs'
$splitBrainTestPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.SplitBrainTests\Program.cs'
$splitBrainScriptPath = Join-Path -Path $repoRoot -ChildPath 'scripts\Test-StateForgeSplitBrain.ps1'

foreach ($splitBrainPath in @(
    $primaryLeaseStorePath,
    $primaryLeaseLockPath,
    $promotionFencePath,
    $splitBrainTestPath,
    $splitBrainScriptPath
)) {
    if (-not (Test-Path -LiteralPath $splitBrainPath)) {
        throw "Missing split-brain prevention file: $splitBrainPath"
    }
}

$primaryLeaseStoreText = Get-Content -LiteralPath $primaryLeaseStorePath -Raw
$primaryLeaseLockText = Get-Content -LiteralPath $primaryLeaseLockPath -Raw
$promotionFenceText = Get-Content -LiteralPath $promotionFencePath -Raw
$splitBrainTestText = Get-Content -LiteralPath $splitBrainTestPath -Raw

if ($primaryLeaseStoreText -notmatch 'File\.Replace' -or
    $primaryLeaseStoreText -notmatch 'InvalidDataException' -or
    $primaryLeaseStoreText -notmatch 'Epoch') {
    throw 'Primary leases must use atomic writes, strict parsing, and monotonic fencing epochs.'
}

if ($primaryLeaseLockText -notmatch 'FileShare\.None' -or
    $primaryLeaseLockText -notmatch 'StateForgeReplicaStateMutex') {
    throw 'Primary lease acquisition must coordinate through both shared-file and local locking.'
}

if ($promotionFenceText -notmatch 'CandidateEligible' -or
    $promotionFenceText -notmatch 'ExistingPrimaryStale' -or
    $promotionFenceText -notmatch 'LeaseId') {
    throw 'Promotion fencing must require quorum, stale-primary checks, and ownership tokens.'
}

if ($splitBrainTestText -notmatch 'concurrent promotion single winner' -or
    $splitBrainTestText -notmatch 'failover safety marker suppression') {
    throw 'Split-brain tests must cover concurrent acquisition and blocked failover markers.'
}

if ($monitoringRunnerText -notmatch "'SplitBrain'") {
    throw 'Test-StateForge.ps1 must expose the SplitBrain suite.'
}

# Validate v0.35.0 multi-site disaster recovery.
$siteStateStorePath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeSiteStateStore.cs'
$crossSiteEvaluatorPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeCrossSiteEvaluator.cs'
$replicationManifestEntryPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.Replication\StateForgeReplicationManifestEntry.cs'
$multiSiteTestPath = Join-Path -Path $repoRoot -ChildPath 'src\StateForge.MultiSiteTests\Program.cs'
$multiSiteScriptPath = Join-Path -Path $repoRoot -ChildPath 'scripts\Test-StateForgeMultiSite.ps1'

foreach ($multiSitePath in @(
    $siteStateStorePath,
    $crossSiteEvaluatorPath,
    $replicationManifestEntryPath,
    $multiSiteTestPath,
    $multiSiteScriptPath
)) {
    if (-not (Test-Path -LiteralPath $multiSitePath)) {
        throw "Missing multi-site disaster recovery file: $multiSitePath"
    }
}

$siteStateStoreText = Get-Content -LiteralPath $siteStateStorePath -Raw
$crossSiteEvaluatorText = Get-Content -LiteralPath $crossSiteEvaluatorPath -Raw
$replicationManifestEntryText = Get-Content -LiteralPath $replicationManifestEntryPath -Raw
$multiSiteTestText = Get-Content -LiteralPath $multiSiteTestPath -Raw

if ($siteStateStoreText -notmatch 'File\.Replace' -or
    $siteStateStoreText -notmatch 'InvalidDataException' -or
    $siteStateStoreText -notmatch 'LastRecoveryPointUtc') {
    throw 'Site state must be atomic, strictly parsed, and include recovery-point metadata.'
}

if ($crossSiteEvaluatorText -notmatch 'RequireDifferentRegion' -or
    $crossSiteEvaluatorText -notmatch 'MaximumRecoveryPointAge' -or
    $crossSiteEvaluatorText -notmatch 'CandidateEligible' -or
    $crossSiteEvaluatorText -match 'EvaluateAndFailover') {
    throw 'Cross-site policy must validate region, freshness, and quorum without automatic failover.'
}

if ($replicationManifestEntryText -notmatch 'SiteName' -or
    $replicationManifestEntryText -notmatch 'Region') {
    throw 'Replication manifests must carry target site identity and region.'
}

if ($multiSiteTestText -notmatch 'multi-site snapshot restore drill' -or
    $multiSiteTestText -notmatch 'cross-site policy root binding') {
    throw 'Multi-site tests must cover restore drills and policy-to-replica binding.'
}

if ($monitoringRunnerText -notmatch "'MultiSite'") {
    throw 'Test-StateForge.ps1 must expose the MultiSite suite.'
}

if ($monitoringRunnerText -notmatch 'StateForgeRepositoryRoot' -or
    $monitoringRunnerText -notmatch 'Missing required validation script' -or
    $monitoringRunnerText -notmatch 'Push-Location') {
    throw 'Test-StateForge.ps1 must resolve scripts from the repository root and fail on missing suites.'
}


# Validate v0.30.3 agent guidance.
$agentsPath = Join-Path -Path $repoRoot -ChildPath 'AGENTS.md'

if (-not (Test-Path -LiteralPath $agentsPath)) {
    throw 'Missing AGENTS.md.'
}

$agentsText = Get-Content -LiteralPath $agentsPath -Raw

if ($agentsText -notmatch 'Windows PowerShell 5.1') {
    throw 'AGENTS.md must document Windows PowerShell 5.1 compatibility requirements.'
}

if ($agentsText -notmatch 'Test-StateForge.ps1 -Suite Production') {
    throw 'AGENTS.md must document Production suite validation.'
}

if ($agentsText -notmatch 'Production Release') {
    throw 'AGENTS.md must document the next roadmap milestone.'
}
