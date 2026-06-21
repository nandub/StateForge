# Testing and Validation

## Build

```powershell
.\scripts\Build-StateForge.ps1
```

## Documentation

```powershell
.\scripts\Test-StateForgeDocs.ps1
.\scripts\Test-StateForge.ps1 -Suite ApiDocs
```

`ApiDocs` restores the repository-pinned DocFX tool, builds generated reference metadata for all thirteen
shipped packages, and verifies that Core, Format, and Security retain complete compiler-enforced XML comments.

## Version Consistency

```powershell
.\scripts\Test-StateForgeVersionConsistency.ps1
```

## Core Format

```powershell
.\scripts\Test-StateForgeFormat.ps1
.\scripts\Test-StateForgeStfg2Envelope.ps1
.\scripts\Test-StateForgeStfg2Migration.ps1
.\scripts\Test-StateForgeStfg2StoreMigration.ps1
```

## Operations

```powershell
.\scripts\Test-StateForgeMaintenanceHost.ps1
.\scripts\Test-StateForgeObservability.ps1
```

## High Availability / Disaster Recovery

```powershell
.\scripts\Test-StateForgeShardingImplementation.ps1
.\scripts\Test-StateForgeReplication.ps1
.\scripts\Test-StateForgeReplicationService.ps1
.\scripts\Test-StateForgeReplicaMonitoring.ps1
.\scripts\Test-StateForgeQuorum.ps1
.\scripts\Test-StateForgeWitness.ps1
.\scripts\Test-StateForgeSplitBrain.ps1
.\scripts\Test-StateForgeMultiSite.ps1
.\scripts\Test-StateForgeDeployment.ps1
.\scripts\Test-StateForgeSnapshotServices.ps1
.\scripts\Test-StateForgeAutomaticFailover.ps1
.\scripts\Test-StateForgeIncrementalSnapshots.ps1
.\scripts\Test-StateForgeRecoveryFlow.ps1
```

## Release Hardening

```powershell
.\scripts\Test-StateForgeHardening.ps1
.\scripts\Test-StateForgeRelease.ps1
```


## Consolidated Runner

StateForge uses a single suite-based validation runner:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Docs
.\scripts\Test-StateForge.ps1 -Suite Version
.\scripts\Test-StateForge.ps1 -Suite ApiDocs
.\scripts\Test-StateForge.ps1 -Suite ApiCompatibility
.\scripts\Test-StateForge.ps1 -Suite UpgradeCompatibility
.\scripts\Test-StateForge.ps1 -Suite Security
.\scripts\Test-StateForge.ps1 -Suite Samples
.\scripts\Test-StateForge.ps1 -Suite Format
.\scripts\Test-StateForge.ps1 -Suite Migration
.\scripts\Test-StateForge.ps1 -Suite Observability
.\scripts\Test-StateForge.ps1 -Suite Maintenance
.\scripts\Test-StateForge.ps1 -Suite Replication
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
.\scripts\Test-StateForge.ps1 -Suite Quorum
.\scripts\Test-StateForge.ps1 -Suite Witness
.\scripts\Test-StateForge.ps1 -Suite SplitBrain
.\scripts\Test-StateForge.ps1 -Suite MultiSite
.\scripts\Test-StateForge.ps1 -Suite Deployment
.\scripts\Test-StateForge.ps1 -Suite Packages
.\scripts\Test-StateForge.ps1 -Suite Snapshots
.\scripts\Test-StateForge.ps1 -Suite Recovery
.\scripts\Test-StateForge.ps1 -Suite Release
.\scripts\Test-StateForge.ps1 -Suite All
```

Feature-specific scripts remain available for compatibility.

## Public API Compatibility

```powershell
.\scripts\Test-StateForge.ps1 -Suite ApiCompatibility
```

This compiles the runtime API smoke test and compares all thirteen package assemblies with the reviewed
files under `api-baselines`. Additions, removals, and signature changes fail until the change is reviewed
and explicitly approved:

```powershell
.\scripts\Test-StateForgeApiCompatibility.ps1 -UpdateBaseline
```

An approved public API change must also update `docs\08-api-reference.md` and `CHANGELOG.md`.

## Rolling Upgrade Compatibility

```powershell
.\scripts\Test-StateForge.ps1 -Suite UpgradeCompatibility
```

The suite uses an independent legacy STFG1 reader/writer fixture. It verifies supported same-shard mixed
reads, writes, refreshes, and removes; legacy replication and snapshot restore; shard fallback and
post-drain migration; and explicit AES and STFG2 downgrade boundaries.

## Security Validation

```powershell
.\scripts\Test-StateForge.ps1 -Suite Security
```

The suite verifies authenticated AES records, full-record tamper rejection, authentication flag
stripping, wrong-key rejection, legacy AES read compatibility, bounded decompression, and validated
atomic key-ring saves.

## Sample Validation

```powershell
.\scripts\Test-StateForge.ps1 -Suite Samples
```

The suite builds all SDK-style samples, verifies direct FileStore persistence across processes, checks
per-folder README coverage, and validates safe ASP.NET Framework sample defaults.

## Package Readiness

```powershell
.\scripts\Test-StateForge.ps1 -Suite Packages
```

This builds all thirteen package and symbol artifacts, validates NuGet repository and commit metadata,
inspects portable PDB SourceLink mappings, and builds isolated `net8.0` and `net481` consumers from the
local package feed.

## Performance Baseline

```powershell
.\scripts\Test-StateForge.ps1 -Suite Performance
.\scripts\Invoke-StateForgePerformanceBaseline.ps1 -Profile All
```

The suite checks durable artifact dependencies, runs focused performance tests, and compares a fresh
small-profile candidate with the reviewed baseline in `performance-baselines`. Candidate reports are
generated under ignored `artifacts\performance`; they are never required inputs.

To intentionally replace reviewed references after evaluating the machine and results:

```powershell
.\scripts\Invoke-StateForgePerformanceBaseline.ps1 -Profile All -UpdateBaseline
```

## Soak Testing

```powershell
.\scripts\Test-StateForge.ps1 -Suite Soak
.\scripts\Invoke-StateForgeSoakTest.ps1 -DurationSeconds 3600 -MaxOperations 100000 -FinalReplication -FinalSnapshot
```

The short `Soak` suite validates the harness and report shape. Release validation includes this short
gate, but Production validation does not run long-duration work by default.

For v1.0 release readiness, run the soak harness against production-like storage and settings for the
planned duration. The workload cycles create/update, read, refresh, lock/update, cleanup, and optional
replication and snapshot operations. Use final replication/snapshot for a stable release evidence run,
or interval replication/snapshot to stress maintenance during active writes. Reports are written to
ignored `artifacts\soak` as JSON and CSV.


## Operational Script Dispatcher

```powershell
.\scripts\Invoke-StateForge.ps1 -Command BuildPackages
```


## Operational Script Guidance

`Invoke-StateForge.ps1` is a convenience runner for low-parameter commands only.

Use direct scripts for parameter-rich operations:

```powershell
.\scripts\Invoke-StateForgeMaintenanceHost.ps1
.\scripts\Start-StateForgeReplicationHost.ps1
.\scripts\New-StateForgeIncrementalSnapshot.ps1
```


## Production Readiness

```powershell
.\scripts\Test-StateForge.ps1 -Suite Production
```

See `docs\12-production-readiness.md` and `docs\13-runbooks.md`.


## Production Non-Interactive Guard

```powershell
.\scripts\Test-StateForgeProductionNonInteractive.ps1
```

This verifies that production validation provides a default `RootPath` to health validation.


## Replica Catch-Up

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
.\scripts\Test-StateForgeReplicaCatchUp.ps1
```

See `docs\14-replica-catch-up.md`.
