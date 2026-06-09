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

# Validate v0.26.2 release hardening requirements.
$requiredHardeningFiles = @(
    'src\StateForge.RecoveryFlowTests\StateForge.RecoveryFlowTests.csproj',
    'scripts\Test-StateForgeRecoveryFlow.ps1',
    'scripts\Test-StateForgeHardening.ps1',
    'docs\release-hardening.md'
)

foreach ($requiredHardeningFile in $requiredHardeningFiles) {
    $requiredHardeningPath = Join-Path -Path $repoRoot -ChildPath $requiredHardeningFile

    if (-not (Test-Path -LiteralPath $requiredHardeningPath)) {
        throw "Missing v0.26.2 hardening file: $requiredHardeningFile"
    }
}

$csprojFiles = Get-ChildItem -Path (Join-Path -Path $repoRoot -ChildPath 'src') -Recurse -Filter '*.csproj'
foreach ($csprojFile in $csprojFiles) {
    $csprojText = Get-Content -LiteralPath $csprojFile.FullName -Raw

    if ($csprojText -notmatch '<Version>0\.26\.2</Version>') {
        throw "Project version must be 0.26.2: $($csprojFile.FullName)"
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
