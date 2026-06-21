# API Reference

This is the human-maintained API guide. It should remain concise and focus on contracts and operational
semantics that are not obvious from individual members.

## Generated Reference

StateForge also produces a Microsoft-style DocFX reference for every shipped package:

```powershell
.\scripts\Build-StateForgeApiDocs.ps1
```

Open [`artifacts\docfx\site\index.html`](index.md) after the build, then select
[Generated .NET API](api/index.md). Public type and member pages are generated from
compiler XML comments. Missing comments fail the build for all thirteen shipped packages.

Selected high-value APIs also include rendered C# examples. The curated set covers direct file storage,
lock-token updates, ASP.NET Framework and ASP.NET Core registration, cloud-native health endpoints,
STFG2 envelopes, AES key-ring lifecycle, telemetry and performance snapshots, Prometheus endpoints,
replication, snapshots, and remote gRPC clients. `Test-StateForgeApiDocs.ps1` verifies that these examples remain present
in their generated API pages.

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

- <xref:StateForge.FileStore.StateForgeFileStore>
- <xref:StateForge.FileStore.StateForgeFileStoreOptions>
- <xref:StateForge.Core.StateForgeStoreStats>
- <xref:StateForge.Core.StateForgeConstants.FlagAuthenticated>

Lock IDs returned by `GetAndLock` are fencing tokens. `SetAndUnlock` succeeds for an existing entry only
when the entry is actively locked with the supplied lock ID. `Refresh` returns `false` for expired entries.
New AES records set `FlagAuthenticated` and carry a record-level HMAC-SHA256 trailer. Readers retain
compatibility with legacy AES records that do not set the flag.

## ASP.NET Core Cache

- <xref:StateForge.AspNetCore.StateForgeDistributedCache>
- <xref:StateForge.AspNetCore.StateForgeDistributedCacheOptions>

The cache adapter preserves sliding expiration and absolute expiration independently. Refresh extends the
sliding window without extending an absolute expiration deadline.

## Remote Store

- <xref:StateForge.Remote.RemoteStateForgeStore>
- <xref:StateForge.Remote.RemoteStateForgeOptions>
- <xref:StateForge.Remote.StateForgeRemoteEndpoint>
- <xref:StateForge.Remote.StateForgeRemoteServiceCollectionExtensions>

`StateForge.Remote` is a client package. The `tcp:HOST:PORT` endpoint form is accepted as a configuration
alias and converted to `https://HOST:PORT` for gRPC/TLS. Client endpoints must name a concrete host or IP;
wildcards such as `*`, `0.0.0.0`, and `[::]` are rejected.

## Replication

- <xref:StateForge.Replication.StateForgeFileReplicator>
- <xref:StateForge.Replication.StateForgeReplicationOptions>
- <xref:StateForge.Replication.StateForgeReplicationResult>
- <xref:StateForge.Replication.StateForgeReplicationManifest>
- <xref:StateForge.Replication.StateForgeReplicaSyncState>
- <xref:StateForge.Replication.StateForgeReplicaStateStore>
- <xref:StateForge.Replication.StateForgeReplicaConfiguration>
- <xref:StateForge.Replication.StateForgeReplicaMonitor>
- <xref:StateForge.Replication.StateForgeReplicaMonitorSnapshot>
- <xref:StateForge.Replication.StateForgeReplicaMonitorEntry>

[StateForgeReplicaMonitor.Capture](xref:StateForge.Replication.StateForgeReplicaMonitor.Capture*) accepts a stale threshold and can accept an explicit capture timestamp
for deterministic evaluation. Missing or stale state is reported as unhealthy.
[StateForgeReplicaConfiguration.Parse](xref:StateForge.Replication.StateForgeReplicaConfiguration.Parse*) accepts semicolon-separated `name=path` or positional path entries.
[StateForgeReplicaStateStore.Read](xref:StateForge.Replication.StateForgeReplicaStateStore.Read*) throws `InvalidDataException` for incomplete or invalid persisted state.

## Quorum

- <xref:StateForge.Replication.StateForgeClusterMember>
- <xref:StateForge.Replication.StateForgeClusterMemberRole>
- <xref:StateForge.Replication.StateForgeQuorumPolicy>
- <xref:StateForge.Replication.StateForgeQuorumEvaluator>
- <xref:StateForge.Replication.StateForgeQuorumResult>

[StateForgeQuorumEvaluator.Evaluate](xref:StateForge.Replication.StateForgeQuorumEvaluator.Evaluate*) calculates available and required votes and evaluates one explicitly
named promotion candidate. A zero `MinimumVotes` uses a strict majority of enabled voting members.
The evaluator does not select candidates, elect leaders, or invoke promotion.

## Witness Nodes

- <xref:StateForge.Replication.StateForgeWitnessNode>
- <xref:StateForge.Replication.StateForgeWitnessState>
- <xref:StateForge.Replication.StateForgeWitnessStateStore>
- <xref:StateForge.Replication.StateForgeWitnessHealthEntry>
- <xref:StateForge.Replication.StateForgeWitnessEvaluator>

[StateForgeWitnessStateStore](xref:StateForge.Replication.StateForgeWitnessStateStore) persists atomic `stateforge-witness-state.json` files.
[StateForgeWitnessEvaluator.Evaluate](xref:StateForge.Replication.StateForgeWitnessEvaluator.Evaluate*) validates heartbeat freshness, witness identity, errors, and a
candidate-specific granted vote. `ToClusterMember` creates a non-promotable witness quorum member whose
availability reflects the validated vote.

## Split-Brain Prevention

- <xref:StateForge.Replication.StateForgePrimaryLease>
- <xref:StateForge.Replication.StateForgePrimaryLeaseStore>
- <xref:StateForge.Replication.StateForgePromotionFenceOptions>
- <xref:StateForge.Replication.StateForgePromotionFenceResult>
- <xref:StateForge.Replication.StateForgePromotionFenceService>

[StateForgePromotionFenceService.Acquire](xref:StateForge.Replication.StateForgePromotionFenceService.Acquire*) requires an eligible quorum result for the exact candidate.
Lease acquisition uses machine-local serialization plus an exclusive shared-file lock. A stale takeover
increments the epoch; active-owner reacquisition and `Renew` require the exact lease ID.

## Multi-Site Disaster Recovery

- <xref:StateForge.Replication.StateForgeSiteRole>
- <xref:StateForge.Replication.StateForgeSiteState>
- <xref:StateForge.Replication.StateForgeSiteStateStore>
- <xref:StateForge.Replication.StateForgeCrossSitePolicy>
- <xref:StateForge.Replication.StateForgeCrossSiteResult>
- <xref:StateForge.Replication.StateForgeCrossSiteEvaluator>

[StateForgeSiteStateStore](xref:StateForge.Replication.StateForgeSiteStateStore) atomically persists strict `stateforge-site-state.json` metadata.
[StateForgeCrossSiteEvaluator.Evaluate](xref:StateForge.Replication.StateForgeCrossSiteEvaluator.Evaluate*) validates site identity, region separation, target health,
heartbeat freshness, recovery-point freshness, promotion eligibility, and quorum for one exact candidate.
It does not elect a site or invoke failover.

## Snapshots

- <xref:StateForge.Snapshots.StateForgeSnapshotService>
- <xref:StateForge.Snapshots.StateForgeSnapshotOptions>
- <xref:StateForge.Snapshots.StateForgeSnapshotResult>
- <xref:StateForge.Snapshots.StateForgeIncrementalSnapshotService>
- <xref:StateForge.Snapshots.StateForgeIncrementalSnapshotOptions>
- <xref:StateForge.Snapshots.StateForgeIncrementalSnapshotResult>

## Promotion / Failover

- <xref:StateForge.Snapshots.StateForgeReplicaPromotionService>
- <xref:StateForge.Snapshots.StateForgeReplicaPromotionOptions>
- <xref:StateForge.Snapshots.StateForgeFailoverService>
- <xref:StateForge.Snapshots.StateForgeFailoverOptions>

Set `RequirePromotionFence` and provide `PromotionFence` to make lease acquisition mandatory. Rejected
fencing returns an error without restoring data or writing promotion/failover markers.
Set `RequireCrossSitePolicy` and provide an eligible `CrossSitePolicy` result to require a policy decision
whose target root exactly matches the selected failover replica.

## Replica Prometheus

- <xref:StateForge.Prometheus.StateForgeReplicaPrometheusCollector>
- <xref:StateForge.Prometheus.StateForgeReplicaPrometheusFormatter>
