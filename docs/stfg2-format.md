# STFG2 File Format

StateForge v0.13.0 adds the STFG2 file-format foundation.

## Purpose

STFG2 is designed to support:

- embedded `KeyId`
- multi-key AES decryption
- checksum validation
- future format upgrades
- migration from STFG1

## Binary Layout

```text
Magic        4 bytes    STFG
Version      1 byte     2
Flags        4 bytes    StateForgeFormatFlags
KeyIdLength  2 bytes    UTF-8 key id length
KeyId        variable   UTF-8 key id
Checksum     32 bytes   SHA-256 of payload
Payload      variable   stored payload
```

## Validate

```powershell
.\scripts\Test-StateForgeFormat.ps1
```

Expected:

```text
PASS: STFG2 write
PASS: STFG2 read
PASS: STFG2 KeyId
PASS: STFG2 checksum
PASS: STFG2 corruption detection
```

## Inspect

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- stfg2-inspect `
    --file .\some-session.stfg2
```

## Current Scope

This release adds the STFG2 primitives and validation harness. It does not yet switch the production FileStore writer from STFG1 to STFG2.
