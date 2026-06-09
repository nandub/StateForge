# API Validation

StateForge v0.17.1 adds an API validation harness.

## Purpose

The API validation harness catches compile-time mismatches against the actual public API shape.

It currently verifies:

- `StateForgeFileStore.Set(string, byte[], TimeSpan)`
- `StateForgeFileStore.Get(string)` returns `StateForgeEntry`
- `StateForgeEntry byte[] payload access`
- `StateForgeFileStore.GetStats()`
- `StateForgePrometheusCollector.CollectText(rootPath)`

## Run

```powershell
.\scripts\Test-StateForgeApiValidation.ps1
```

## Why This Exists

The v0.17.0 scale test initially assumed:

```csharp
store.Set(key, payload, DateTimeOffset)
byte[] value = store.Get(key)
```

The actual API is:

```csharp
store.Set(key, payload, TimeSpan)
StateForgeEntry entry = store.Get(key)
```

This harness prevents that class of regression.


## v0.17.2 Correction

`StateForgeEntry` does not expose a `Payload` property. The API validation harness now uses reflection-safe byte-array detection so it validates the real entry model instead of assuming a property name.

Detected byte-array property during packaging: `Value`.
