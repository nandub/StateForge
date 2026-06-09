# Performance and Scale

StateForge v0.17.0 adds scale validation tooling.

## Fast Scale Test

```powershell
.\scripts\Test-StateForgeScale.ps1
```

Default:

```text
Sessions     : 2000
PayloadBytes : 512
Threads      : 4
```

## Manual Large Scale Test

```powershell
.\scripts\Test-StateForgeLargeScale.ps1 -Sessions 100000
```

## Custom Scale Test

```powershell
.\scripts\Invoke-StateForgeScaleTest.ps1 `
    -RootPath D:\StateForgeScale `
    -Sessions 25000 `
    -PayloadBytes 1024 `
    -Threads 8 `
    -Keep
```

## Measured Scenarios

- concurrent create
- concurrent read
- statistics scan
- Prometheus collection
- cleanup with no expired records

## Exit Criteria

A scale test is successful when:

- all sessions are created
- all sessions are readable
- store statistics match expected session count
- Prometheus output includes session metrics
- cleanup completes without failure


## v0.17.1 API Correction

The scale harness uses the actual FileStore API:

```csharp
store.Set(key, payload, TimeSpan.FromHours(1));
StateForgeEntry entry = store.Get(key);
```


## v0.17.2 Entry Payload Correction

The scale harness no longer assumes `StateForgeEntry.Payload`.

Detected byte-array property during packaging: `Value`.
