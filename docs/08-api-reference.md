# API Reference

This is the human-maintained API guide. It should remain concise and focus on contracts and operational
semantics that are not obvious from individual members.

## Generated Reference

StateForge also produces a Microsoft-style DocFX reference for every shipped package:

```powershell
.\scripts\Build-StateForgeApiDocs.ps1
```

Open `artifacts\docfx\site\README.html` after the build. Public type and member pages are generated from
compiler XML comments. Missing comments fail the build for the completed Core, Format, and Security
documentation slice; the remaining package comments will be hardened incrementally.

Selected high-value APIs also include rendered C# examples. The initial curated set covers direct file
storage, lock-token updates, ASP.NET Core registration, STFG2 envelopes, AES key-ring lifecycle,
Prometheus endpoints, replication, and snapshots. `Test-StateForgeApiDocs.ps1` verifies that these
examples remain present in their generated API pages.

Implementation references are cited only where they materially inform behavior:

- STFG2 payload checksums use SHA-256 as specified by [NIST FIPS 180-4](https://csrc.nist.gov/pubs/fips/180-4/upd1/final).
- AES key sizes follow [NIST FIPS 197](https://csrc.nist.gov/pubs/fips/197/final).
- Authenticated STFG1 records use HMAC-SHA256 following the HMAC construction in [RFC 2104](https://www.rfc-editor.org/rfc/rfc2104).

These are standards references, not claims that StateForge source code was copied from those publications.

## Compatibility Policy

Every shipped package has a reviewed machine-readable signature file under `api-baselines`.
`Test-StateForgeApiCompatibility.ps1` rejects public type, constructor, field, constant, property, event,
method, inheritance, and enum-value drift.

For an intentional public API change:

1. Review the compatibility failure and versioning impact.
2. Update this document and `CHANGELOG.md`.
3. Run `.\scripts\Test-StateForgeApiCompatibility.ps1 -UpdateBaseline`.
4. Review the baseline diff before committing it.

Baseline updates are approvals, not automatic formatting steps.

## File Store

- `StateForgeFileStore`
- `StateForgeFileStoreOptions`
- `StateForgeStoreStats`
- `StateForgeConstants.FlagAuthenticated`

Lock IDs returned by `GetAndLock` are fencing tokens. `SetAndUnlock` succeeds for an existing entry only
when the entry is actively locked with the supplied lock ID. `Refresh` returns `false` for expired entries.
New AES records set `FlagAuthenticated` and carry a record-level HMAC-SHA256 trailer. Readers retain
compatibility with legacy AES records that do not set the flag.

## ASP.NET Core Cache

- `StateForgeDistributedCache`
- `StateForgeDistributedCacheOptions`

The cache adapter preserves sliding expiration and absolute expiration independently. Refresh extends the
sliding window without extending an absolute expiration deadline.

## Replication

- `StateForgeFileReplicator`
- `StateForgeReplicationOptions`
- `StateForgeReplicationResult`
- `StateForgeReplicationManifest`
- `StateForgeReplicaSyncState`
- `StateForgeReplicaStateStore`
- `StateForgeReplicaConfiguration`
- `StateForgeReplicaMonitor`
- `StateForgeReplicaMonitorSnapshot`
- `StateForgeReplicaMonitorEntry`

`StateForgeReplicaMonitor.Capture` accepts a stale threshold and can accept an explicit capture timestamp
for deterministic evaluation. Missing or stale state is reported as unhealthy.
`StateForgeReplicaConfiguration.Parse` accepts semicolon-separated `name=path` or positional path entries.
`StateForgeReplicaStateStore.Read` throws `InvalidDataException` for incomplete or invalid persisted state.

## Quorum

- `StateForgeClusterMember`
- `StateForgeClusterMemberRole`
- `StateForgeQuorumPolicy`
- `StateForgeQuorumEvaluator`
- `StateForgeQuorumResult`

`StateForgeQuorumEvaluator.Evaluate` calculates available and required votes and evaluates one explicitly
named promotion candidate. A zero `MinimumVotes` uses a strict majority of enabled voting members.
The evaluator does not select candidates, elect leaders, or invoke promotion.

## Witness Nodes

- `StateForgeWitnessNode`
- `StateForgeWitnessState`
- `StateForgeWitnessStateStore`
- `StateForgeWitnessHealthEntry`
- `StateForgeWitnessEvaluator`

`StateForgeWitnessStateStore` persists atomic `stateforge-witness-state.json` files.
`StateForgeWitnessEvaluator.Evaluate` validates heartbeat freshness, witness identity, errors, and a
candidate-specific granted vote. `ToClusterMember` creates a non-promotable witness quorum member whose
availability reflects the validated vote.

## Split-Brain Prevention

- `StateForgePrimaryLease`
- `StateForgePrimaryLeaseStore`
- `StateForgePromotionFenceOptions`
- `StateForgePromotionFenceResult`
- `StateForgePromotionFenceService`

`StateForgePromotionFenceService.Acquire` requires an eligible quorum result for the exact candidate.
Lease acquisition uses machine-local serialization plus an exclusive shared-file lock. A stale takeover
increments the epoch; active-owner reacquisition and `Renew` require the exact lease ID.

## Multi-Site Disaster Recovery

- `StateForgeSiteRole`
- `StateForgeSiteState`
- `StateForgeSiteStateStore`
- `StateForgeCrossSitePolicy`
- `StateForgeCrossSiteResult`
- `StateForgeCrossSiteEvaluator`

`StateForgeSiteStateStore` atomically persists strict `stateforge-site-state.json` metadata.
`StateForgeCrossSiteEvaluator.Evaluate` validates site identity, region separation, target health,
heartbeat freshness, recovery-point freshness, promotion eligibility, and quorum for one exact candidate.
It does not elect a site or invoke failover.

## Snapshots

- `StateForgeSnapshotService`
- `StateForgeSnapshotOptions`
- `StateForgeSnapshotResult`
- `StateForgeIncrementalSnapshotService`
- `StateForgeIncrementalSnapshotOptions`
- `StateForgeIncrementalSnapshotResult`

## Promotion / Failover

- `StateForgeReplicaPromotionService`
- `StateForgeReplicaPromotionOptions`
- `StateForgeFailoverService`
- `StateForgeFailoverOptions`

Set `RequirePromotionFence` and provide `PromotionFence` to make lease acquisition mandatory. Rejected
fencing returns an error without restoring data or writing promotion/failover markers.
Set `RequireCrossSitePolicy` and provide an eligible `CrossSitePolicy` result to require a policy decision
whose target root exactly matches the selected failover replica.

## Replica Prometheus

- `StateForgeReplicaPrometheusCollector`
- `StateForgeReplicaPrometheusFormatter`
