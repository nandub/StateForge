# STFG2 Migration

StateForge v0.13.3 adds the first STFG2 migration harness and CLI support.

## Purpose

The migration layer converts a legacy payload file into an STFG2 envelope without changing the production FileStore writer.

This is useful for validating migration behavior before enabling STFG2 as a production writer format.

## Validate

```powershell
.\scripts\Test-StateForgeStfg2Migration.ps1
```

Expected:

```text
PASS: legacy payload migration
PASS: STFG2 destination creation
PASS: migrated KeyId
PASS: migrated checksum
PASS: existing STFG2 passthrough
```

## CLI Migration

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- stfg2-migrate `
    --source .\legacy.bin `
    --destination .\legacy.stfg2 `
    --key-id key-004 `
    --overwrite
```

## Inspect Migrated File

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- stfg2-inspect `
    --file .\legacy.stfg2
```

## Current Scope

This release migrates individual files. It does not yet perform full recursive StateForge store migration.

Next logical step:

```text
stfg2-migrate-store --root D:\StateForge --key-id key-004
```
