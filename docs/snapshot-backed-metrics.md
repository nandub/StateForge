# Snapshot-Backed Metrics

StateForge v0.20.0 adds snapshot-backed operational metrics.

## Problem

Full-store operations such as stats, Prometheus collection, and cleanup scans become visible at large session counts.

## Solution

Capture a store snapshot periodically, then let monitoring endpoints read the snapshot.

This changes monitoring from repeated full-store enumeration to a small JSON read.

## Commands

Create a snapshot:

```powershell
.\scripts\New-StateForgeSnapshot.ps1 `
    -RootPath D:\StateForge `
    -SnapshotPath .\artifacts\snapshots\store.json
```

Prometheus from snapshot:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- prometheus-snapshot `
    --snapshot .\artifacts\snapshots\store.json
```

## Kestrel Harness

```text
POST /stateforge/snapshot
GET  /stateforge/prometheus-snapshot
```

Environment variable:

```text
STATEFORGE_SNAPSHOT_PATH
```

## Validation

```powershell
.\scripts\Test-StateForgeSnapshotMetrics.ps1
```
