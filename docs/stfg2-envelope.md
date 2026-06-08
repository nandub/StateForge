# STFG2 Envelope

StateForge v0.13.2 adds an opt-in STFG2 envelope compatibility layer.

## Purpose

The envelope layer allows StateForge to validate STFG2 wrapping and unwrapping without changing the default production FileStore behavior.

This keeps STFG1 stable while preparing the codebase for future STFG2 migration.

## Added APIs

```csharp
StateForgeStfg2Envelope.Wrap(...)
StateForgeStfg2Envelope.Unwrap(...)
```

## Behavior

| Input | Result |
|---|---|
| STFG2 payload | Parses header, validates checksum, returns KeyId and payload |
| STFG1/legacy payload | Passes bytes through unchanged |

## Validate

```powershell
.\scripts\Test-StateForgeStfg2Envelope.ps1
```

Expected:

```text
PASS: STFG2 envelope wrap
PASS: STFG2 envelope unwrap
PASS: STFG2 envelope KeyId
PASS: STFG2 envelope flags
PASS: STFG1 compatibility passthrough
```

## Create Sample STFG2 File

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- stfg2-create `
    --out .\sample.stfg2 `
    --key-id key-003 `
    --text "hello"
```

Inspect:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- stfg2-inspect `
    --file .\sample.stfg2
```

## Current Scope

v0.13.2 does not yet switch the FileStore writer to STFG2. It adds the compatibility layer and validates the behavior needed for that migration.
