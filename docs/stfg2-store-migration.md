# STFG2 Store Migration

StateForge v0.13.5 adds recursive store migration.

## Purpose

This command scans a StateForge store recursively and wraps legacy `.stfg` files in STFG2 envelopes.

Existing STFG2 files are detected and skipped.

## Dry Run

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- stfg2-migrate-store `
    --root D:\StateForge `
    --key-id key-004 `
    --dry-run
```

## Apply

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- stfg2-migrate-store `
    --root D:\StateForge `
    --key-id key-004 `
    --apply
```

## Backup Behavior

When `--apply` is used, each migrated file gets a backup:

```text
session.stfg.stfg1.bak
```

## Validate Harness

```powershell
.\scripts\Test-StateForgeStfg2StoreMigration.ps1
```

Expected:

```text
PASS: store migration dry-run
PASS: store migration apply
PASS: store migration backups
PASS: store migration KeyId
PASS: store migration second-pass skip
```

## Current Limit

This migration wraps raw files as STFG2 envelopes. It does not yet interpret full StateForge session metadata or decrypt/re-encrypt payloads through the key ring.
