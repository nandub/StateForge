# Disaster Recovery

## Quorum Before Promotion

`StateForgeQuorumEvaluator` can be used by operators or future recovery orchestration to confirm that the
configured voting members have quorum and that a named replica satisfies promotion policy. Version 0.32.0
does not automatically connect this result to failover or leader election.

## Witness Vote Validation

`StateForgeWitnessEvaluator` validates `stateforge-witness-state.json` against a heartbeat threshold and
the intended candidate. Convert a validated result with `ToClusterMember` before quorum evaluation.
Version 0.33.0 does not automatically invoke failover from a witness vote.

## Split-Brain Prevention

Version 0.34.0 adds `stateforge-primary-lease.json` in a shared lease root. A fenced promotion requires:

- a quorum result for the exact candidate with `HasQuorum` and `CandidateEligible`
- an expired lease, no lease, or the matching lease ownership token
- exclusive access to the shared lease lock
- a positive lease duration

New owners receive a new lease ID and a monotonically increasing epoch. Existing owners must present the
lease ID to reacquire or renew an active lease. Expired leases cannot be renewed.

## Multi-Site Recovery

Version 0.35.0 persists `stateforge-site-state.json` in each site root. It records site identity, region,
primary or recovery role, health, heartbeat, promotion eligibility, recovery-point time, and errors.

`StateForgeCrossSiteEvaluator` requires an enabled primary source, an enabled recovery target, distinct
site identities and regions, a healthy and fresh recovery point, and quorum for the exact replica candidate.
The result is policy evidence only. Set `RequireCrossSitePolicy` to bind it to the selected replica root,
and continue to require promotion fencing for cross-site failover.

StateForge disaster recovery is built from snapshots, incremental deltas, promotion, and failover.

## v1 Release DR Drill

Run this drill before approving a v1.0 production release and after material changes to replication,
snapshot, promotion, failover, fencing, site-state, encryption, sharding, or storage layout.

The drill is release evidence, not just a unit-test pass. Execute it against production-like storage and
archive the generated reports, manifests, marker files, and validation command output.

### Preconditions

- The primary store is writable and contains representative session data.
- At least one named replica is configured with stable site and region metadata.
- A snapshot repository is available outside the live session root.
- The shared promotion lease root is visible to every promotion coordinator.
- The recovery site has a current `stateforge-site-state.json` with a fresh recovery point.
- Operators know the exact replica candidate and do not rely on automatic site election.
- Store root, snapshot repository, key-ring, and lease-root ACLs have been reviewed.

### Drill Phases

1. Record baseline state:
   - package version and commit
   - primary root
   - replica root and name
   - site names and regions
   - encryption, compression, shard depth, and key-ring location
2. Run production validation:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Production
```

3. Confirm replica health and catch-up:

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaMonitoring
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
```

4. Create or verify the recovery snapshot chain:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Snapshots
```

5. Restore the selected snapshot chain into an isolated drill root and verify session counts and
   application-readable state.
6. Evaluate quorum, witness vote, and promotion fencing for the exact replica candidate:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Quorum
.\scripts\Test-StateForge.ps1 -Suite Witness
.\scripts\Test-StateForge.ps1 -Suite SplitBrain
```

7. Evaluate the cross-site policy for the exact recovery site and replica root:

```powershell
.\scripts\Test-StateForge.ps1 -Suite MultiSite
```

8. Simulate primary loss, promote the selected replica or restored snapshot, and require:
   - `RequirePromotionFence = true`
   - a matching quorum result
   - a matching cross-site policy result for cross-site recovery
   - no active rival primary lease
9. Validate the promoted root:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Recovery
.\scripts\Invoke-StateForgeSmokeTest.ps1
```

10. Run the post-recovery production gate:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Production
```

### Required Evidence

Archive the following with the release record:

- command transcript for every validation command
- `stateforge-site-state.json` from primary and recovery sites
- `stateforge-replica-state.json` for each replica considered for promotion
- replication manifest with target site and region metadata
- full and incremental snapshot manifests used in the drill
- restored drill-root session count and application-read result
- quorum evaluation result and witness vote result
- promotion fence result with lease ID, owner, and epoch
- `promotion-marker.json` or `failover-marker.json` from the promoted root
- cross-site policy result when the drill crosses regions
- post-recovery health, smoke, and Production validation output
- start time, end time, recovery-point age, and recovery duration

### Release-Blocking Conditions

Block v1.0 release approval until reviewed if any of these occur:

- replica catch-up reports content drift that is not explained by expected writes
- snapshot restore loses sessions or restores outside the configured destination
- quorum, witness, or cross-site policy does not target the exact selected candidate
- promotion fencing is bypassed, unavailable, or returns a rival active lease
- failed promotion or failover writes a marker
- promoted root fails health, smoke, or Production validation
- recovery-point age exceeds the deployment target
- any required evidence artifact is missing

## Full Snapshots

A full snapshot copies the complete `sessions` directory and writes a manifest.

## Incremental Snapshots

Incremental snapshots compare a current store to a parent snapshot and write only changed files plus delete markers.
Changed files are detected with SHA256 content hashes rather than timestamps or file length alone.

Snapshot names must be single directory names. Snapshot creation and restore reject rooted paths,
directory traversal, and manifest entries that resolve outside their configured repository or destination.

Delta actions:

| Action | Meaning |
|---|---|
| `add` | New file exists in source but not parent |
| `modify` | File exists in both but changed |
| `delete` | File existed in parent but no longer exists |

Validation:

```powershell
.\scripts\Test-StateForgeIncrementalSnapshots.ps1
```

## Replica Promotion

Replica promotion restores a replica or snapshot into a new primary root and writes a promotion marker
only after the restore succeeds. Set `RequirePromotionFence` and provide `PromotionFence` to reject an
unfenced promotion before restore.

## Automatic Failover

Automatic failover checks primary health, rejects unreadable or invalid session files, selects a usable
replica, promotes it, and writes a failover marker only after promotion succeeds. When fencing is required,
an active primary or missing quorum rejects failover without writing markers.

## Recovery Flow

Validated end-to-end flow:

```text
replication -> snapshot -> restore -> promotion -> failover
```

Validation:

```powershell
.\scripts\Test-StateForgeRecoveryFlow.ps1
```

Release evidence drill:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Production
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
.\scripts\Test-StateForge.ps1 -Suite MultiSite
.\scripts\Test-StateForge.ps1 -Suite Recovery
```
