# Production Readiness

StateForge v0.35.0 includes multi-site disaster-recovery validation in the production-readiness layer.

## Production Suite

Run:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Production
```

This suite validates:

- documentation shape
- version consistency
- replica lag monitoring
- quorum and promotion eligibility policy
- witness heartbeat and candidate vote validation
- primary lease, fencing epoch, and stale-primary validation
- site metadata, cross-site policy, restore drill, and fenced site failover
- Docker and Kubernetes deployment invariants
- NuGet metadata, symbols, SourceLink, and install compatibility
- reviewed public API compatibility
- rolling-upgrade and migration compatibility
- authenticated AES record and key-ring security validation
- maintained sample build and behavior validation
- reviewed performance baseline and substantial-regression checks
- repository layout
- source guards
- health checks
- smoke tests
- observability
- replication
- snapshots
- recovery flow
- package metadata

## Purpose

The production suite is not a replacement for full release validation. It is a practical pre-flight check before promoting StateForge builds to a production-like environment.

## Recommended Order

```powershell
.\scripts\Build-StateForge.ps1
.\scripts\Test-StateForge.ps1 -Suite Production
.\scripts\Invoke-StateForge.ps1 -Command BuildPackages
```

## Dedicated Operational Scripts

Continue to use direct scripts for parameter-heavy operations:

```powershell
.\scripts\Invoke-StateForgeMaintenanceHost.ps1
.\scripts\Start-StateForgeReplicationHost.ps1
.\scripts\New-StateForgeSnapshot.ps1
.\scripts\New-StateForgeIncrementalSnapshot.ps1
```


## Non-Interactive Execution

Production validation is designed to run unattended. The suite provides a temporary health-check root automatically and does not prompt for `RootPath`.

Default health-check root:

```text
%TEMP%\StateForgeProductionHealth
```

This allows the suite to run in CI/CD, scheduled jobs, and release gates.


## Replica Catch-Up

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
.\scripts\Test-StateForgeReplicaCatchUp.ps1
```

See `docs\14-replica-catch-up.md`.

## Replica Monitoring

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
```

The suite validates atomic and concurrent state updates, strict corrupt-state handling, stale-threshold
boundaries, named multi-replica configuration, dashboard reporting, and Prometheus output.

## Quorum Foundations

```powershell
.\scripts\Test-StateForge.ps1 -Suite Quorum
```

The suite validates majority and explicit thresholds, unavailable voters, candidate voting requirements,
replica-role checks, invalid configurations, and the no-election boundary.

## Witness Nodes

```powershell
.\scripts\Test-StateForge.ps1 -Suite Witness
```

The suite validates atomic state, strict corrupt-state handling, heartbeat freshness, identity matching,
candidate-specific votes, quorum restoration, and witness promotion rejection.

## Split-Brain Prevention

```powershell
.\scripts\Test-StateForge.ps1 -Suite SplitBrain
```

The suite validates atomic strict lease state, shared-file coordination, quorum rejection, active-primary
fencing, stale takeover, epoch advancement, ownership-token renewal, concurrent candidates, and failover
marker suppression.

## Multi-Site Disaster Recovery

```powershell
.\scripts\Test-StateForge.ps1 -Suite MultiSite
```

The suite validates atomic strict site state, site-tagged replication manifests, region separation,
heartbeat and recovery-point freshness, exact quorum candidate binding, snapshot restore drills, fenced
cross-site failover, and rejection when policy evidence targets a different replica root.

## Docker and Kubernetes

```powershell
.\scripts\Test-StateForge.ps1 -Suite Deployment
docker build --tag stateforge-kestrel:0.35.0 .
```

The image runs as the .NET `app` user, listens on port `8080`, and uses `/data/stateforge` for cache data
and snapshot metrics. Demo `/session` and `/health` harness endpoints are disabled by default.

The Kubernetes manifests require a `ReadWriteMany` storage class for multiple replicas, Metrics Server
for the HPA, and pod security support for UID/GID `1654`. Replace the local image name with a
registry-qualified image in remote clusters.

Encryption is disabled in the generic ConfigMap. To enable AES, populate `stateforge-secret` with
`STATEFORGE_AES_KEY_BASE64`, then set `STATEFORGE_ENCRYPTION=true` and
`STATEFORGE_PROTECTION_MODE=aes`. Do not commit the key.

## Package and SourceLink Validation

```powershell
.\scripts\Test-StateForge.ps1 -Suite Packages
```

The package suite builds all twelve `.nupkg` and `.snupkg` artifacts, checks repository URL and commit
metadata, validates portable PDB SourceLink mappings, and restores plus builds isolated `net8.0` and
`net481` consumer projects from the local package feed.

## Performance Baseline

```powershell
.\scripts\Test-StateForge.ps1 -Suite Performance
.\scripts\Invoke-StateForgePerformanceBaseline.ps1 -Profile All
```

The tracked references cover:

| Profile | Sessions | Payload | Threads | Intended use |
|---|---:|---:|---:|---|
| small | 250 | 512 bytes | 2 | Fast Production and CI regression gate |
| medium | 1,000 | 1,024 bytes | 4 | Routine release comparison |
| large | 3,000 | 4,096 bytes | 8 | Pre-release capacity review |

Each profile measures concurrent create, read, lock/update, and refresh operations plus statistics,
Prometheus collection, cleanup, full replication, and full snapshot creation. Reports include latency
percentiles, throughput, store bytes, and managed-memory growth.

The automated small-profile gate intentionally uses broad relative limits: at least 15 percent of
reviewed throughput and P95 no greater than eight times reference plus 25 ms. This catches major
regressions while tolerating workstation and CI variability. It is not an application SLA.

For deployment sizing, rerun all profiles on the intended storage class, operating system, encryption
mode, compression setting, shard depth, and backup policy. Use measured P95/P99 latency, filesystem
capacity, replication duration, and snapshot duration to set limits. Validate the expected peak
concurrency and session payload distribution with a longer soak test before production promotion.

Reviewed inputs are committed under `performance-baselines`. Generated candidates remain under ignored
`artifacts\performance`, so clean-clone validation never depends on local build output.

## Soak Testing

```powershell
.\scripts\Test-StateForge.ps1 -Suite Soak
.\scripts\Invoke-StateForgeSoakTest.ps1 -DurationSeconds 21600 -MaxOperations 1000000 -FinalReplication -FinalSnapshot
```

The short `Soak` suite proves the harness. The v1.0 production decision should use a reviewed
long-duration run on production-like storage, encryption, compression, sharding, backup, replication,
and snapshot settings. Prefer final replication/snapshot for the release evidence run; add interval
replication/snapshot for a separate stress run if active-write maintenance behavior is under review.
Treat any data-verification failure, unhandled workload error, or report missing
from `artifacts\soak` as release-blocking until understood.

## Public API Compatibility

```powershell
.\scripts\Test-StateForge.ps1 -Suite ApiCompatibility
```

The suite runs exact API smoke compilation and compares all shipped package surfaces with reviewed
baselines. Any addition, removal, enum-value change, inheritance change, or member signature change
blocks Production and Release validation until explicitly reviewed and approved.

## Rolling Upgrade Compatibility

```powershell
.\scripts\Test-StateForge.ps1 -Suite UpgradeCompatibility
```

The supported mixed-version path keeps all nodes on the same shard depth and STFG1 live-store layout.
The suite validates bidirectional reads and writes on that path, along with refresh, remove, replication,
and snapshot restore. Shard-depth migration occurs only after older writers are drained. AES records and
STFG2 envelopes are explicit downgrade boundaries.

## Security Validation

```powershell
.\scripts\Test-StateForge.ps1 -Suite Security
```

New AES records use AES-CBC plus an encrypt-then-MAC HMAC-SHA256 tag over the complete serialized STFG1
record. Current readers reject changed metadata, ciphertext, tags, unknown flags, authentication flag
stripping, wrong keys, trailing bytes, oversized records, and decompressed payloads beyond
`MaxPayloadBytes`. Existing unauthenticated AES records remain readable for migration.

Key-ring saves validate the complete ring before writing and replace existing files atomically. Store
roots and key-ring files still require operating-system access controls; encryption does not replace
least-privilege filesystem permissions.
