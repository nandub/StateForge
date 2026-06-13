# StateForge

StateForge is a resilient, file-backed session and state platform for .NET applications.

Current version: **0.34.0**

## Features

- File-backed session/state storage
- STFG/STFG2 format support
- ASP.NET Framework provider
- ASP.NET Core provider
- Security envelope helpers
- Telemetry and Prometheus metrics
- Directory sharding
- Replication services
- Replica lag monitoring
- Quorum and promotion eligibility policy
- Witness health and vote validation
- Primary leases and promotion fencing
- Snapshot services
- Snapshot scheduling
- Replica promotion
- Automatic failover
- Incremental snapshots
- Recovery-flow validation

## Quick Start

```powershell
.\scripts\Build-StateForge.ps1
.\scripts\Test-StateForgeDocs.ps1
.\scripts\Test-StateForgeVersionConsistency.ps1
.\scripts\Test-StateForgeIncrementalSnapshots.ps1
```

## Documentation

See [docs/README.md](docs/README.md).

Core docs:

- [Getting Started](docs/01-getting-started.md)
- [Architecture](docs/02-architecture.md)
- [Disaster Recovery](docs/03-disaster-recovery.md)
- [Observability](docs/04-observability.md)
- [Testing](docs/05-testing.md)
- [Solution Layout](docs/06-solution-layout.md)
- [Roadmap](docs/07-roadmap.md)
- [API Reference](docs/08-api-reference.md)
- [Release History](docs/09-release-history.md)
- [Contributing](docs/10-contributing.md)

## Validation

```powershell
.\scripts\Build-StateForge.ps1
.\scripts\Test-StateForgeHardening.ps1
.\scripts\Test-StateForgeRelease.ps1
```


## Consolidated Script Runner

```powershell
.\scripts\Test-StateForge.ps1 -Suite Docs
.\scripts\Test-StateForge.ps1 -Suite Snapshots
.\scripts\Test-StateForge.ps1 -Suite Release
```

Operational dispatcher:

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


## Replica Catch-Up

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
.\scripts\Test-StateForgeReplicaCatchUp.ps1
```

See `docs\14-replica-catch-up.md`.


## Coding Agents

See [AGENTS.md](AGENTS.md) for repository instructions for Codex, Claude, and other coding agents.
