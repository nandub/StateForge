# StateForge Documentation

StateForge documentation is intentionally consolidated into a small set of durable guides.

## Core Documents

| Document | Purpose |
|---|---|
| [01-getting-started.md](01-getting-started.md) | Build, run, configure, deploy, and operate StateForge |
| [02-architecture.md](02-architecture.md) | Storage, format, sharding, replication, snapshots, and failover architecture |
| [03-disaster-recovery.md](03-disaster-recovery.md) | Snapshots, incremental snapshots, promotion, failover, and recovery flow |
| [04-observability.md](04-observability.md) | Telemetry, Prometheus, metrics, dashboards, and snapshot-backed metrics |
| [05-testing.md](05-testing.md) | Build validation, feature validation, hardening, and release checks |
| [06-solution-layout.md](06-solution-layout.md) | Project catalog and responsibilities |
| [07-roadmap.md](07-roadmap.md) | Completed milestones and planned roadmap |
| [08-api-reference.md](08-api-reference.md) | Public service and model reference |
| [09-release-history.md](09-release-history.md) | Release timeline |
| [10-contributing.md](10-contributing.md) | Contribution and validation expectations |

## Policy

Do not add one document per feature unless the feature becomes large enough to justify its own guide. Prefer updating one of the consolidated documents.


| [11-script-reference.md](11-script-reference.md) | Consolidated validation and operational script usage |


`11-script-reference.md` documents the validation runner and convenience command runner.


| [12-production-readiness.md](12-production-readiness.md) | Production-readiness validation |
| [13-runbooks.md](13-runbooks.md) | Operational runbooks for failover, restore, rolling upgrades, and package verification |

| [14-replica-catch-up.md](14-replica-catch-up.md) | Replica catch-up and resynchronization foundations |

## Generated .NET API

Build the Microsoft-style generated reference for all twelve shipped packages:

```powershell
.\scripts\Build-StateForgeApiDocs.ps1
```

The site is written to `artifacts\docfx\site`. The conceptual API guide remains
`08-api-reference.md`; DocFX derives type and member pages from compiler XML comments.

## Coding Agents

Repository guidance for Codex, Claude, and other coding agents is available in
[`AGENTS.md`](https://github.com/nandub/StateForge/blob/main/AGENTS.md).
