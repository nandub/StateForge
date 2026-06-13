# Changelog

## 0.32.0

- Added cluster member and role models for quorum evaluation.
- Added majority and explicit minimum-vote quorum policies.
- Added deterministic promotion-candidate eligibility checks.
- Added diagnostics for lost quorum and rejected candidates.
- Added the `Quorum` validation suite to Production and Release validation.
- Kept leader election and automatic promotion integration out of this foundation release.

## 0.31.1

- Serialized replica sync state updates with a path-scoped cross-process mutex.
- Added strict validation for incomplete, malformed, and unsupported replica state files.
- Added named replica configuration using `name=path` entries while preserving positional path compatibility.
- Added replica health, lag, counters, and errors to the dashboard command.
- Added stale-threshold boundary, concurrent update, corrupt state, configuration, and metric label tests.

## 0.31.0

- Added durable per-replica sync state under `stateforge-replica-state.json`.
- Added successful sync timestamps, failed sync counters, and catch-up operation counters.
- Added deterministic replica lag calculation and stale-threshold evaluation.
- Added multi-replica health snapshots.
- Added labeled Prometheus metrics for lag, health, last sync, catch-up operations, and failed syncs.
- Added optional Kestrel metrics integration through `STATEFORGE_REPLICA_ROOTS`.
- Added the `ReplicaMonitoring` validation suite to Release and Production validation.

## 0.30.4

- Added snapshot repository path containment before copy or recursive deletion.
- Added incremental manifest path containment during restore.
- Changed incremental snapshot drift detection to SHA256 content hashing.
- Enforced lock IDs as fencing tokens after stale-lock recovery.
- Prevented refresh from reviving expired entries.
- Preserved sliding and absolute expiration semantics in the ASP.NET Core distributed-cache adapter.
- Suppressed promotion and failover markers when recovery does not complete successfully.
- Strengthened failover health checks to reject unreadable or invalid session files.
- Added regression coverage for the hardening changes.

## 0.30.3

- Added automatic failover primitives.
- Added primary health evaluation.
- Added replica selection and failover marker generation.
- Added `Test-StateForgeAutomaticFailover.ps1`.

## 0.25.0

- Added replica promotion primitives.
- Added promotion marker generation.
- Added `Test-StateForgeReplicaPromotion.ps1`.

## 0.24.0

- Added snapshot scheduling primitives.
- Added snapshot retention support.
- Added `Test-StateForgeSnapshotScheduling.ps1`.

## 0.23.0

- Added `StateForge.Snapshots`.
- Added snapshot create/list/restore.
- Added snapshot manifests.
- Added `StateForge.SnapshotServiceTests`.
- Added `Test-StateForgeSnapshotServices.ps1`.


## 0.22.1

- Added `StateForge.Replication.Host`.
- Added replication dry-run mode.
- Added replication manifest output.
- Added basic conflict detection.
- Added `StateForge.ReplicationHostTests`.
- Added `Test-StateForgeReplicationService.ps1`.
- Added `Start-StateForgeReplicationHost.ps1`.
- Added `docs/replication-services.md`.


## 0.21.0

- Merged validated v0.19.1 sharding into the v0.20.0 operational baseline.
- Added `StateForge.Replication`.
- Added replication planner.
- Added replication health checks.
- Added file-level primary-to-replica fanout.
- Added sharded layout preservation during replication.
- Added `StateForge.ReplicationTests`.
- Added `Test-StateForgeReplication.ps1`.
- Added `Invoke-StateForgeReplication.ps1`.
- Added `docs/replication-foundations.md`.


## 0.20.0

- Added snapshot-backed Prometheus formatter.
- Added snapshot-backed Prometheus collector.
- Added `StateForge.SnapshotTests`.
- Added `Test-StateForgeSnapshotMetrics.ps1`.
- Added `New-StateForgeSnapshot.ps1`.
- Added `StateForge.Tools snapshot` command.
- Added `StateForge.Tools prometheus-snapshot` command.
- Added Kestrel `POST /stateforge/snapshot`.
- Added Kestrel `GET /stateforge/prometheus-snapshot`.
- Added `docs/snapshot-backed-metrics.md`.


## 0.18.2

- Added sharding analysis helpers.
- Added `StateForgeShardAnalyzer`.
- Added `StateForgeShardAnalysisResult`.
- Added `Test-StateForgeSharding.ps1`.
- Added `docs/sharding-analysis.md`.

## 0.18.1

- Added `StateForge.Performance`.
- Added store snapshot-cache helpers.
- Added `StateForgeStoreSnapshot`.
- Added `StateForgeStoreSnapshotCache`.
- Added `StateForge.PerformanceTests`.
- Added `Test-StateForgePerformance.ps1`.
- Added `docs/snapshot-cache.md`.


## 0.18.0

- Added JSON benchmark export support to `StateForge.ScaleTests`.
- Added CSV benchmark export support to `StateForge.ScaleTests`.
- Added P50/P95/P99 latency reporting.
- Added `Compare-StateForgeBenchmark.ps1`.
- Updated `Test-StateForgeScale.ps1` to emit benchmark artifacts.
- Added `docs/benchmark-exports.md`.


## 0.17.2

- Fixed `StateForge.ScaleTests` to avoid assuming `StateForgeEntry.Payload`.
- Fixed `StateForge.ApiValidationTests` to avoid assuming `StateForgeEntry.Payload`.
- Added reflection-safe byte-array extraction for `StateForgeEntry` validation.
- Updated API validation and performance documentation.
- Detected entry byte-array property during packaging: `Value`.


## 0.17.1

- Fixed `StateForge.ScaleTests` to call `store.Set(..., TimeSpan)`.
- Fixed `StateForge.ScaleTests` to treat `store.Get(...)` as returning `StateForgeEntry`.
- Added `StateForge.ApiValidationTests`.
- Added `Test-StateForgeApiValidation.ps1`.
- Added source validation for ScaleTests FileStore API usage.
- Added `docs/api-validation.md`.


## 0.17.0

- Added `StateForge.ScaleTests`.
- Added `Invoke-StateForgeScaleTest.ps1`.
- Added `Test-StateForgeScale.ps1`.
- Added `Test-StateForgeLargeScale.ps1`.
- Added fast and manual large-scale validation paths.
- Added performance and scale documentation.
- Updated release validation to include the fast scale test.


## 0.16.2

- Added `StateForge.Prometheus`.
- Added Prometheus formatter and reflection-safe collector.
- Added Kestrel `/stateforge/prometheus` endpoint.
- Added `dashboard` command to StateForge.Tools using isolated variable names.
- Added `prometheus` command to StateForge.Tools.
- Added `StateForge.PrometheusTests`.
- Added observability validation scripts.
- Added Prometheus, Grafana, and observability documentation.


## 0.15.1

- Added `README-NUGET.md`.
- Added NuGet package README metadata.
- Added license, repository, project URL, and package tags to package-oriented projects.
- Added symbol package settings.
- Updated package build script for v0.15.1.
- Added `Test-StateForgePackageMetadata.ps1`.
- Added `docs/nuget-packaging.md`.


## 0.15.0

- Fixed Maintenance Host JSON output so `healthy` is `null` when health did not run.
- Added package metadata to package-oriented projects.
- Added `Build-StateForgePackages.ps1`.
- Added `Test-StateForgeRelease.ps1`.
- Added `docs/release-packaging.md`.


## 0.14.1

- Added Maintenance Host explicit job selection: cleanup-only, health-only, stats-only, migration-only.
- Added Maintenance Host configuration validation mode.
- Added `Test-StateForgeMaintenanceConfig.ps1`.
- Added `Test-StateForgeMaintenanceTask.ps1`.
- Added log rotation configuration fields.
- Expanded Maintenance Host documentation.


## 0.14.0

- Added `StateForge.Maintenance.Host`.
- Added Maintenance Host once and loop modes.
- Added JSON output support.
- Added JSON config file support.
- Added optional STFG2 migration job support.
- Added Scheduled Task registration scripts.
- Added `Test-StateForgeMaintenanceHost.ps1`.
- Added `docs/maintenance-host.md`.


## 0.13.5

- Added recursive STFG2 store migration.
- Added `StateForgeStfg2StoreMigrator`.
- Added `StateForgeStfg2StoreMigrationResult`.
- Added `stfg2-migrate-store` CLI command.
- Added `StateForge.StoreMigrationHarness`.
- Added `Test-StateForgeStfg2StoreMigration.ps1`.
- Added `docs/stfg2-store-migration.md`.


## 0.13.4

- Fixed STFG2 migration harness payload mismatch caused by UTF-8 BOM-sensitive test data.
- Added source validation to prevent BOM-sensitive migration harness test writes.


## 0.13.3

- Added `StateForgeStfg2Migrator`.
- Added `StateForgeStfg2MigrationResult`.
- Added `StateForge.MigrationHarness`.
- Added `Test-StateForgeStfg2Migration.ps1`.
- Added `stfg2-migrate` CLI command.
- Added `docs/stfg2-migration.md`.


## 0.13.2

- Added opt-in STFG2 envelope compatibility layer.
- Added `StateForgeStfg2Envelope`.
- Added `StateForge.FormatHarness`.
- Added `Test-StateForgeStfg2Envelope.ps1`.
- Added `stfg2-create` CLI command.
- Added `docs/stfg2-envelope.md`.


## 0.13.1

- Fixed `StateForge.Tools` missing `using System.IO;` for `stfg2-inspect`.
- Added source validation for File API imports.


## 0.13.0

- Added `StateForge.Format`.
- Added STFG2 flags, serializer, parser, KeyId support, and SHA-256 checksum validation.
- Added `StateForge.FormatTests`.
- Added `stfg2-inspect` CLI command.
- Added `Test-StateForgeFormat.ps1`.
- Added `docs/stfg2-format.md`.


## 0.12.0

- Added key-ring JSON reader.
- Added key-ring validation command.
- Added key-ring rotation command.
- Added `Rotate-StateForgeKeyRing.ps1`.
- Added `Test-StateForgeKeyRing.ps1`.
- Added `StateForge.Maintenance`.
- Added `Invoke-StateForgeMaintenance.ps1`.
- Added `docs/maintenance.md`.

## 0.11.1

- Added STFG2 KeyId-aware file-format planning primitives.
- Added `StateForgeKeyedPayload`.
- Added `StateForgeKeyRingCryptoPlan`.


## 0.11.0

- Added `StateForge.Security`.
- Added AES key-ring model.
- Added AES key metadata model.
- Added AES key-ring JSON writer.
- Added AES key generation and validation helpers.
- Added `keyring-create` and `keyring-generate-key` CLI commands.
- Added `New-StateForgeKeyRing.ps1`.
- Added `docs/key-rotation.md`.


## 0.10.1

- Added runtime telemetry recording to the Kestrel harness.
- Added `StateForgeTelemetryScope` for safe future instrumentation.
- Added `Test-StateForgeTelemetry.ps1`.
- Updated telemetry documentation with runtime validation steps.


## 0.10.0

- Added `StateForge.Telemetry`.
- Added `StateForge.Telemetry.AspNetCore`.
- Added StateForge EventSource provider.
- Added DiagnosticSource helper.
- Added in-process metric snapshot counters.
- Added ASP.NET Core telemetry endpoints.
- Added `metrics` command to StateForge.Tools.
- Added `docs/telemetry.md`.


## 0.9.1

- Fixed `StateForge.CloudNative` build compatibility with existing option classes.
- Removed unsupported `StaleLockTimeout` assignment.
- Updated environment option binding to use safe property detection.
- Fixed ASP.NET Core distributed cache registration to use `StateForgeDistributedCacheOptions`.


## 0.9.0

- Added `StateForge.CloudNative`.
- Added environment-variable configuration helpers.
- Added `/livez`, `/readyz`, and `/healthz` endpoint helpers.
- Added Dockerfile and `.dockerignore`.
- Added Kubernetes manifests.
- Added `docs/cloud-native.md`.


## 0.8.7

- Expanded ASP.NET Core provider documentation into a complete step-by-step guide.
- Added complete `ExampleService` registration and usage.
- Added Minimal API, controller, and ASP.NET Core Session examples.
- Reviewed and simplified operational docs for clearer step-by-step usage.


## 0.8.6

- Rewrote `Test-StateForgeLayout.ps1` with clean Windows PowerShell 5.1 syntax.
- Fixed parser error caused by a trailing comma in the required-file array.
- Kept documentation folder consolidated to the canonical documentation set.


## 0.8.5

- Consolidated the `docs` folder into the canonical documentation set.
- Removed one-off version fix documents from the documentation folder.
- Merged useful operational notes into troubleshooting, testing, CLI, and architecture docs.
- Updated layout validation to require only the canonical documentation set.


## 0.8.4

- Expanded README into a complete operational landing page.
- Added getting-started documentation.
- Added architecture documentation.
- Added configuration documentation.
- Added ASP.NET provider documentation.
- Added ASP.NET Core provider documentation.
- Added Kestrel harness documentation.
- Added encryption documentation.
- Added farm-mode documentation.
- Added CLI reference.
- Added testing documentation.
- Added benchmarking documentation.
- Added troubleshooting documentation.
- Added production deployment documentation.


## 0.8.3

- Fixed ASP.NET provider harness null `HttpContext` usage.
- Added synthetic `HttpContext` creation to `StateForge.AspNetHarness`.
- Improved ASP.NET harness failure diagnostics.
- Fixed Kestrel harness root path handling in PowerShell script.
- Added harness-fix documentation.


## 0.8.2

- Fixed Kestrel harness missing `using` directives.
- Fixed Kestrel client test missing `using` directives.
- Removed nullable annotation syntax from Kestrel harness projects.
- Extended source validation for Kestrel harness source patterns.


## 0.8.1

- Regenerated `StateForge.sln` with one unique entry per project.
- Added `Repair-StateForgeSolution.ps1`.
- Fixed ASP.NET provider support for `keepBackups`.
- Added solution maintenance documentation.


## 0.8.0

- Added `StateForge.AspNetHarness`.
- Added direct classic ASP.NET provider lifecycle harness.
- Added `StateForge.KestrelHarness`.
- Added `StateForge.KestrelClientTest`.
- Added Kestrel start/test scripts.
- Added non-IIS harness documentation.


## 0.7.5

- Smoke-test output now prints the deterministic AES demo key.
- Smoke-test output now includes AES-aware list and stats commands.
- Added `Show-StateForgeSmokeDemo.ps1`.
- Added AES enumeration documentation.


## 0.7.4

- Added `--protection none|dpapi|aes` to `StateForge.Tools`.
- Added `--aes-key` to `StateForge.Tools`.
- Added AES-aware `list`, `stats`, `health`, `validate`, `cleanup`, and `remove` support.
- Updated JSON list output to include `aesEncrypted`.
- Added tools AES support documentation.


## 0.7.3

- Restored `StringArrayJson()` in `StateForge.Tools`.
- Restored `CreateAesStore()` in `StateForge.SmokeTests`.
- Extended `Test-StateForgeSource.ps1` to catch both helper regressions.
- Added regression-fix documentation.


## 0.7.2

- Added `demo-aes` to the consolidated smoke-test demo store.
- Added `demo-compressed-aes` to the consolidated smoke-test demo store.
- Added demo-store documentation.
- Extended source validation to check AES demo records.


## 0.7.1

- Fixed missing `CanWriteDirectory()` helper in `StateForgeFileStore`.
- Extended source validation to check for `CanWriteDirectory()`.
- Added v0.7.1 build-fix documentation.


## 0.7.0

- Added `StateForgeValidationResult`.
- Added `StateForgeHealthResult`.
- Added `ValidateConfiguration()` admin API.
- Added `CheckHealth()` admin API.
- Added `validate` and `health` commands to `StateForge.Tools`.
- Added `Test-StateForgeHealth.ps1`.
- Added health validation documentation.


## 0.6.1

- Regenerated `StateForge.sln`.
- Removed duplicate `StateForge.ResilienceTests` solution entry.


## 0.6.0

- Added `StateForge.ResilienceTests`.
- Added `Invoke-StateForgeResilienceTest.ps1`.
- Added stale-lock/crash-recovery simulation.
- Added high-session-count statistics validation.
- Added provider-style operation sequence test.
- Added resilience testing documentation.


## 0.5.0

- Added `StateForgeStoreStats`.
- Added `GetStats()` admin API.
- Added `stats` command to `StateForge.Tools`.
- Added `StateForge.FarmTests`.
- Added `Invoke-StateForgeFarmTest.ps1`.
- Added local AES farm simulation.
- Added farm testing documentation.


## 0.4.2

- Correctly inserted `ResolveProtectionMode()` into `StateForgeFileStore`.
- Added `Test-StateForgeSource.ps1`.
- Build and test scripts now run source validation before restore/build/test.


## 0.4.1

- Fixed missing `ResolveProtectionMode()` method in `StateForgeFileStore`.
- Added v0.4.1 build-fix documentation.


## 0.4.0

- Added `StateForgeProtectionMode`.
- Added AES payload protection mode.
- Added AES metadata flag.
- Added `AesKeyBase64` option.
- Added `generate-key` command to `StateForge.Tools`.
- Added AES benchmark support.
- Added AES smoke-test coverage.
- Added protection mode documentation.


## 0.3.3

- Benchmark harness now disables backup creation by default.
- Added `--keep-backups` / `-KeepBackups` benchmark option.
- Added backup behavior documentation.
- Updated benchmarking documentation with backup/no-backup comparison commands.


## 0.3.2

- Fixed false `Solution validation failed` caused by checking `$LASTEXITCODE` after helper PowerShell scripts.
- Rewrote `Build-StateForge.ps1` to only check `$LASTEXITCODE` after native `dotnet` commands.
- Rewrote `Test-StateForge.ps1` with the same safe pattern.
- Improved `Test-StateForgeSolution.ps1` duplicate name/path diagnostics.


## 0.3.1

- Regenerated `StateForge.sln` with unique project entries.
- Fixed duplicate `StateForge.Benchmarks` solution project entry.
- Added `Test-StateForgeSolution.ps1`.
- Build script now validates the solution before restore/build.


## 0.3.0

- Added `StateForge.Benchmarks`.
- Added `Invoke-StateForgeBenchmark.ps1`.
- Added benchmark scenarios for create, read, update, concurrent read, concurrent update, enumeration, and cleanup.
- Added `docs/benchmarking.md`.


## 0.2.1

- Smoke-test output now prints inspectable paths.
- Added consolidated demo store.
- Added `-SkipDemo` support to the smoke-test launcher.
- Updated local smoke-test documentation.


## 0.2.0

- Added `StateForge.SmokeTests`.
- Added `Invoke-StateForgeSmokeTest.ps1`.
- Added local smoke tests for persistence, compression, encryption, locking, stale-lock recovery, cleanup, corruption quarantine, and ASP.NET Core cache behavior.
- Added `docs/local-smoke-testing.md`.


## 0.1.12

- Added `global.json`.
- Pinned the build to .NET SDK 8 using `8.0.100` with `rollForward: latestFeature`.
- Added SDK pinning documentation.


## 0.1.11

- Added missing `System.Configuration` reference to `StateForge.AspNet`.
- Changed `StateForgeFileStoreOptions` from `sealed` to inheritable so `StateForgeDistributedCacheOptions` can derive from it.
- Removed duplicated admin method declarations from `IStateForgeStore`.
- Kept admin methods on `IStateForgeAdminStore`.


## 0.1.10

- Added repository-level `NuGet.config`.
- Updated build script to use `--configfile .\NuGet.config`.
- Added `Test-NuGetSources.ps1`.
- Added restore troubleshooting documentation.

## 0.1.9

- Added `IStateForgeAdminStore`.
- Added JSON output support to `StateForge.Tools`.
- Added `remove --key` command.
- Added IIS deployment guide.
- Added `Install-StateForgeStore.ps1`.

## 0.1.8

- Added repository layout validation.
- Added package metadata.
- Added DPAPI security notes.
- Added `UseWindowsDpapi` option.

## 0.1.7

- Corrected compression/encryption pipeline.
- Added `System.Security.Cryptography.ProtectedData` package reference.
- Added combined compression + encryption test.
