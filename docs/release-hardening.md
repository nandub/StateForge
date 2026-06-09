# Release Hardening

StateForge v0.26.2 is a stabilization release.

## Purpose

This release validates the high-availability chain added through v0.26.1.

## Hardening Checks

Run:

```powershell
.\scripts\Test-StateForgeHardening.ps1
```

This validates:

- repository layout
- source guards
- snapshot marker serialization
- replication -> snapshot -> restore -> promotion -> failover recovery flow
- package metadata when available

## Recovery Flow

Run:

```powershell
.\scripts\Test-StateForgeRecoveryFlow.ps1
```

The recovery flow validates:

1. primary to replica replication
2. snapshot creation
3. snapshot restore
4. replica promotion
5. automatic failover
