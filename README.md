# StateForge

StateForge is a persistent, file-backed session state and distributed cache provider for classic ASP.NET Framework applications and ASP.NET Core applications.

It is designed for environments where session state should survive process crashes, application pool recycles, service restarts, and server maintenance events without requiring SQL Server, Redis, or the built-in ASP.NET State Service.

## Status

Current version: **0.18.2**

StateForge is currently in pre-release validation. The core storage engine, ASP.NET Core cache adapter, classic ASP.NET SessionState provider, diagnostics, smoke tests, farm tests, resilience tests, Kestrel harness, and ASP.NET provider harness have all been added and are under active validation.

## What StateForge Provides

StateForge includes:

- Persistent file-backed session storage
- Atomic file replacement
- Session locking
- Stale lock recovery
- Expiration cleanup
- Corruption quarantine
- Optional compression
- Optional DPAPI encryption
- Optional AES encryption
- File sharding
- Diagnostics
- Statistics
- Health checks
- Classic ASP.NET `SessionStateStoreProviderBase` provider
- ASP.NET Core `IDistributedCache` adapter
- CLI inspection tools
- Smoke tests
- Benchmark harness
- Farm simulation harness
- Resilience test harness
- Classic ASP.NET provider harness without IIS
- Kestrel harness for ASP.NET Core validation without IIS

## Repository Layout

```text
StateForge/
│
├── src/
│   ├── StateForge.Core
│   ├── StateForge.FileStore
│   ├── StateForge.AspNet
│   ├── StateForge.AspNetCore
│   ├── StateForge.Tools
│   ├── StateForge.SmokeTests
│   ├── StateForge.Benchmarks
│   ├── StateForge.FarmTests
│   ├── StateForge.ResilienceTests
│   ├── StateForge.AspNetHarness
│   ├── StateForge.KestrelHarness
│   └── StateForge.KestrelClientTest
│
├── tests/
│   └── StateForge.FileStore.Tests
│
├── scripts/
│   ├── Build-StateForge.ps1
│   ├── Repair-StateForgeSolution.ps1
│   ├── Invoke-StateForgeSmokeTest.ps1
│   ├── Invoke-StateForgeBenchmark.ps1
│   ├── Invoke-StateForgeFarmTest.ps1
│   ├── Invoke-StateForgeResilienceTest.ps1
│   ├── Invoke-StateForgeAspNetHarness.ps1
│   ├── Start-StateForgeKestrelHarness.ps1
│   ├── Test-StateForgeKestrelHarness.ps1
│   ├── Test-StateForgeHealth.ps1
│   └── Show-StateForgeSmokeDemo.ps1
│
└── docs/
```

## Requirements

| Component | Requirement |
|---|---|
| Windows PowerShell | 5.1 |
| .NET SDK | 8.0 or later |
| Classic ASP.NET target | .NET Framework 4.8.1 |
| ASP.NET Core target | ASP.NET Core / .NET 8 |
| IIS | Optional for real deployment |
| Kestrel | Supported through ASP.NET Core |
| OS | Windows Server 2016 or newer recommended |

## Build

From the repository root:

```powershell
.\scripts\Repair-StateForgeSolution.ps1
.\scripts\Test-StateForgeSource.ps1
.\scripts\Build-StateForge.ps1
```

Expected result:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Core Smoke Test

```powershell
.\scripts\Invoke-StateForgeSmokeTest.ps1 -RootPath ..\StateForgeSmoke -Keep
```

This validates:

- FileStore round-trip
- Persistence across store recreation
- Compression
- DPAPI encryption
- AES encryption
- Compression plus encryption
- Lock contention
- Stale lock recovery
- Expiration cleanup
- Corruption quarantine
- ASP.NET Core `IDistributedCache` adapter
- Consolidated demo store

Inspect the demo store:

```powershell
.\scripts\Show-StateForgeSmokeDemo.ps1 -RootPath ..\StateForgeSmoke
```

## Classic ASP.NET Provider Harness

The ASP.NET harness validates the provider lifecycle without IIS:

```powershell
.\scripts\Invoke-StateForgeAspNetHarness.ps1 -RootPath ..\StateForgeAspNetHarness -Keep
```

Expected operations:

```text
PASS: CreateUninitializedItem
PASS: GetItem
PASS: GetItemExclusive
PASS: SetAndReleaseItemExclusive
PASS: ResetItemTimeout
PASS: RemoveItem
```

## Kestrel Harness

Start the ASP.NET Core Kestrel harness:

```powershell
.\scripts\Start-StateForgeKestrelHarness.ps1 -RootPath ..\StateForgeKestrel -Url http://localhost:5075
```

In a second terminal:

```powershell
.\scripts\Test-StateForgeKestrelHarness.ps1 -Url http://localhost:5075
```

Expected result:

```text
PASS: Kestrel health
PASS: Kestrel set
PASS: Kestrel get
PASS: Kestrel delete
```

## Classic ASP.NET IIS Configuration

Copy these assemblies to your web application's `bin` folder:

```text
StateForge.AspNet.dll
StateForge.FileStore.dll
StateForge.Core.dll
```

Example `web.config`:

```xml
<configuration>
  <system.web>
    <sessionState
      mode="Custom"
      customProvider="StateForge"
      timeout="20">

      <providers>
        <add
          name="StateForge"
          type="StateForge.AspNet.StateForgeSessionStateProvider, StateForge.AspNet"
          rootPath="D:\StateForge"
          enableCompression="true"
          enableEncryption="false"
          keepBackups="false"
          defaultTimeoutMinutes="20"
          staleLockMinutes="5"
          shardDepth="1" />
      </providers>
    </sessionState>
  </system.web>
</configuration>
```

## ASP.NET Core Configuration

Register StateForge as an `IDistributedCache` provider:

```csharp
builder.Services.AddStateForgeDistributedCache(options =>
{
    options.RootPath = @"D:\StateForge";
    options.EnableCompression = true;
    options.EnableEncryption = false;
    options.ShardDepth = 1;
    options.KeepBackups = false;
});
```

## Encryption Modes

StateForge supports three protection modes:

| Mode | Purpose |
|---|---|
| None | Fastest; use when storage is protected by disk encryption or server ACLs |
| DPAPI | Good for single-server Windows deployments |
| AES | Recommended for web farms and shared storage |

Generate an AES key:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- generate-key
```

Inspect AES-protected records:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- list `
    --root D:\StateForge `
    --format json `
    --protection aes `
    --aes-key "<base64-key>"
```

## CLI Tools

Common commands:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- diag --root D:\StateForge
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- list --root D:\StateForge --format json
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- stats --root D:\StateForge --format json
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- validate --root D:\StateForge
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- health --root D:\StateForge
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- cleanup --root D:\StateForge
```

## Farm Simulation

```powershell
$key = dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- generate-key

.\scripts\Invoke-StateForgeFarmTest.ps1 `
    -RootPath ..\StateForgeFarm `
    -AesKeyBase64 $key `
    -Keep
```

This validates:

```text
NodeA writes
NodeB reads
NodeC locks and updates
NodeD reads updated value
```

## Resilience Testing

```powershell
.\scripts\Invoke-StateForgeResilienceTest.ps1 `
    -RootPath ..\StateForgeResilience `
    -Sessions 10000 `
    -Keep
```

This validates:

- Lock stealing after simulated crash
- Store recreation after simulated process restart
- High-session-count statistics
- Provider-style operation sequence

## Benchmarking

```powershell
.\scripts\Invoke-StateForgeBenchmark.ps1 `
    -RootPath ..\StateForgeBench `
    -Sessions 10000 `
    -PayloadBytes 4096 `
    -Threads 8 `
    -Compression `
    -Keep
```

Benchmark with AES:

```powershell
$key = dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- generate-key

.\scripts\Invoke-StateForgeBenchmark.ps1 `
    -RootPath ..\StateForgeBenchAes `
    -Sessions 10000 `
    -PayloadBytes 4096 `
    -Threads 8 `
    -Compression `
    -Aes `
    -AesKeyBase64 $key `
    -Keep
```

## Production Guidance

Recommended storage path:

```text
D:\StateForge
```

Recommended folders:

```text
D:\StateForge\sessions
D:\StateForge\temp
D:\StateForge\backups
D:\StateForge\quarantine
```

Recommended antivirus exclusions:

```text
D:\StateForge\sessions
D:\StateForge\temp
```

Recommended production defaults:

```text
enableCompression=true
keepBackups=false
shardDepth=1
staleLockMinutes=5
```

For web farms, use AES with the same key on every server.

## Documentation Map

- `docs/getting-started.md`
- `docs/architecture.md`
- `docs/configuration.md`
- `docs/aspnet-provider.md`
- `docs/aspnetcore-provider.md`
- `docs/kestrel-harness.md`
- `docs/encryption.md`
- `docs/farm-mode.md`
- `docs/cli-reference.md`
- `docs/testing.md`
- `docs/benchmarking.md`
- `docs/troubleshooting.md`
- `docs/production-deployment.md`

## Cloud-Native Profile

StateForge v0.9.0 adds `StateForge.CloudNative`, environment-variable configuration, `/livez`, `/readyz`, `/healthz`, Docker support, and Kubernetes manifests. See `docs/cloud-native.md`.

## Telemetry

StateForge v0.10.0 adds `StateForge.Telemetry`, EventSource events, DiagnosticSource support, metric snapshots, CLI metrics, and ASP.NET Core telemetry endpoints. See `docs/telemetry.md`.

## Runtime Telemetry Validation

With Kestrel running:

```powershell
.\scripts\Test-StateForgeTelemetry.ps1 -Url http://localhost:5075
```

## AES Key Ring

StateForge v0.11.0 adds `StateForge.Security` and AES key-ring management foundations. See `docs/key-rotation.md`.

## Maintenance

StateForge v0.12.0 adds `StateForge.Maintenance` and `Invoke-StateForgeMaintenance.ps1` for cleanup, health, and stats jobs. See `docs/maintenance.md`.

## STFG2 File Format

StateForge v0.13.0 adds `StateForge.Format`, STFG2 KeyId support, checksum validation, and format tests. See `docs/stfg2-format.md`.

## v0.13.1

Fixes the `StateForge.Tools` STFG2 inspection build issue by adding the missing `System.IO` import.

## STFG2 Envelope

StateForge v0.13.2 adds an opt-in STFG2 envelope layer for compatibility validation. See `docs/stfg2-envelope.md`.

## STFG2 Migration

StateForge v0.13.3 adds `StateForgeStfg2Migrator`, `StateForge.MigrationHarness`, and the `stfg2-migrate` CLI command. See `docs/stfg2-migration.md`.

## v0.13.4

Fixes the STFG2 migration harness payload mismatch by writing the legacy test payload as raw UTF-8 bytes instead of using `File.WriteAllText(..., Encoding.UTF8)`.

## STFG2 Store Migration

StateForge v0.13.5 adds recursive `stfg2-migrate-store` dry-run/apply support. See `docs/stfg2-store-migration.md`.

## Maintenance Host

StateForge v0.14.0 adds `StateForge.Maintenance.Host`, once/loop mode, JSON output, config support, and Scheduled Task helper scripts. See `docs/maintenance-host.md`.

## v0.14.1 Maintenance Host Hardening

Adds explicit job selection, config validation, log rotation fields, and Scheduled Task helper validation.

## Release Packaging

StateForge v0.15.0 adds NuGet package build scripts and release-readiness validation. See `docs/release-packaging.md`.

## NuGet Packaging Polish

StateForge v0.15.1 adds NuGet README, license, repository metadata, symbol package settings, and package metadata validation. See `docs/nuget-packaging.md`.

## Observability Release

StateForge v0.16.2 adds dashboard CLI output and Prometheus text exposition support. See `docs/observability.md`.

## Performance and Scale Release

StateForge v0.17.0 adds scale validation tooling for large stores, concurrent create/read tests, stats scans, Prometheus collection, and cleanup timing. See `docs/performance-scale.md`.

## v0.17.1

Fixes the scale harness FileStore API usage and adds API validation tests. See `docs/api-validation.md`.

## v0.17.2

Fixes API validation and scale tests so they no longer assume `StateForgeEntry.Payload`.

## v0.18.0

Adds benchmark JSON/CSV exports, P50/P95/P99 latency reporting, and benchmark comparison tooling.

## v0.18.1

Adds snapshot-cache performance helpers.

## v0.18.2

Adds sharding analysis helpers and validation scripts.
