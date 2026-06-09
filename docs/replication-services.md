# Replication Services

StateForge v0.22.1 promotes the replication foundation into an operational service layer.

## Components

- `StateForge.Replication.Host`
- replication dry-run mode
- replication manifest output
- conflict detection
- service validation harness
- host runner script

## Validate

```powershell
.\scripts\Test-StateForgeReplication.ps1
.\scripts\Test-StateForgeReplicationService.ps1
```

## Run Host Once

```powershell
.\scripts\Start-StateForgeReplicationHost.ps1 `
    -PrimaryRootPath D:\StateForgePrimary `
    -ReplicaRootPath D:\ReplicaA,D:\ReplicaB `
    -ManifestPath .\artifacts\replication\manifest.json
```

## Dry Run

```powershell
.\scripts\Start-StateForgeReplicationHost.ps1 `
    -PrimaryRootPath D:\StateForgePrimary `
    -ReplicaRootPath D:\ReplicaA `
    -ManifestPath .\artifacts\replication\dry-run.json `
    -DryRun
```

## Manifest

The manifest records:

- relative session path
- source length
- source last-write UTC
- replica name
- destination path
- action
- reason

## Conflict Detection

v0.22.1 detects basic conflicts when a destination file exists with a different length or newer timestamp.

This is intentionally conservative.
