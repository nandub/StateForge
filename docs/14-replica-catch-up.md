# Replica Catch-Up and Resynchronization

StateForge v0.30.3 introduces replica catch-up foundations.

## Purpose

A replica can fall behind when it misses file changes, is offline, or is restored from an older snapshot. Replica catch-up compares primary and replica session files, plans the differences, and optionally applies the required changes.

## Supported Actions

| Action | Meaning |
|---|---|
| `copy-missing` | File exists on primary but not replica |
| `copy-changed` | File exists on both but length or timestamp differs |
| `delete-extra` | File exists on replica but not primary |

## Dry Run

Dry run is the default.

```powershell
.\scripts\Test-StateForgeReplicaCatchUp.ps1
```

## Validation Suite

```powershell
.\scripts\Test-StateForge.ps1 -Suite ReplicaCatchUp
```

## Safety Model

- Planning does not modify the replica.
- Apply mode must be explicitly requested by setting `DryRun = false`.
- Extra replica files are deleted only when `DeleteExtraReplicaFiles = true`.
- Existing parameter-rich operations remain dedicated scripts.


## Change Detection

v0.30.3 uses SHA256 content hashing for changed-file detection. File length alone is not sufficient, and timestamp comparison can be unreliable because timestamp resolution differs across filesystems and copy paths.


## Deterministic Test Fixtures

v0.30.3 changed replica catch-up tests to write deterministic `.stfg` files directly. This keeps the tests focused on catch-up planning and content-drift detection instead of relying on FileStore path-generation behavior.

The test suite now validates:

- same relative path
- same byte length
- different file content
- SHA256 detects drift
- apply mode converges the replica
