# Disaster Recovery

## Quorum Before Promotion

`StateForgeQuorumEvaluator` can be used by operators or future recovery orchestration to confirm that the
configured voting members have quorum and that a named replica satisfies promotion policy. Version 0.32.0
does not automatically connect this result to failover or leader election.

## Witness Vote Validation

`StateForgeWitnessEvaluator` validates `stateforge-witness-state.json` against a heartbeat threshold and
the intended candidate. Convert a validated result with `ToClusterMember` before quorum evaluation.
Version 0.33.0 does not automatically invoke failover from a witness vote.

StateForge disaster recovery is built from snapshots, incremental deltas, promotion, and failover.

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
only after the restore succeeds.

## Automatic Failover

Automatic failover checks primary health, rejects unreadable or invalid session files, selects a usable
replica, promotes it, and writes a failover marker only after promotion succeeds.

## Recovery Flow

Validated end-to-end flow:

```text
replication -> snapshot -> restore -> promotion -> failover
```

Validation:

```powershell
.\scripts\Test-StateForgeRecoveryFlow.ps1
```
