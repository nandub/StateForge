# AES Key Ring and Rotation

StateForge v0.11.0 adds the AES key-ring foundation.

## Current Scope

This release adds:

- `StateForge.Security`
- AES key metadata model
- AES key-ring model
- AES key-ring JSON writer
- key generation helper
- key-ring validation helper
- CLI commands to create key-ring material

The storage engine still writes with a single AES key. Full multi-key read/write and migration are planned for the next phase.

## Create a Key Ring

```powershell
.\scripts\New-StateForgeKeyRing.ps1 `
    -OutFile .\stateforge-keyring.json `
    -KeyId key-001
```

or:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- keyring-create `
    --out .\stateforge-keyring.json `
    --key-id key-001
```

## Generate a Single AES Key Entry

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- keyring-generate-key `
    --key-id key-002
```

JSON output:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- keyring-generate-key `
    --key-id key-002 `
    --format json
```

## Key Ring Format

```json
{
  "version": "1",
  "currentKeyId": "key-001",
  "keys": [
    {
      "keyId": "key-001",
      "keyBase64": "...",
      "createdUtc": "2026-05-31T00:00:00.0000000Z",
      "notBeforeUtc": "2026-05-31T00:00:00.0000000Z",
      "retiredUtc": null
    }
  ]
}
```

## Intended Rotation Model

Future releases should use this pattern:

1. Read all keys in the ring.
2. Write new sessions with `currentKeyId`.
3. Read existing sessions with the key ID embedded in the file metadata.
4. Retire old keys only after old sessions expire or are migrated.
5. Optionally re-encrypt old sessions with the new current key.

## Operational Guidance

- Store key rings in a secret store.
- Do not commit key rings to source control.
- Back up key rings separately from session files.
- Use one shared key ring across every farm node.
- Rotate keys during a maintenance window until live rotation is implemented.


## v0.11.1 / v0.12.0 Additions

Added:

- key-ring JSON reader
- key-ring validation command
- key-ring rotation command
- key-ring rotation script
- key-ring test script
- STFG2 planning constants for future KeyId-aware file format

Validate:

```powershell
.\scripts\Test-StateForgeKeyRing.ps1 -OutFile .\stateforge-keyring-test.json
```

Rotate:

```powershell
.\scripts\Rotate-StateForgeKeyRing.ps1 `
    -RingFile .\stateforge-keyring.json `
    -NewKeyId key-002
```
