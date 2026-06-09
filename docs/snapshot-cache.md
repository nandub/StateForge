# Store Snapshot Cache

StateForge v0.18.1 introduces snapshot-cache helpers.

## Purpose

v0.18.0 benchmarks showed that full-store operations such as stats, Prometheus collection, and cleanup become visible around 25,000 sessions.

Snapshot-cache tooling captures store statistics into a JSON file so operational tools can use a recent snapshot instead of repeatedly scanning the entire store.

## Validate

```powershell
.\scripts\Test-StateForgePerformance.ps1
```

## Current Scope

v0.18.1 adds the library and validation harness. It does not yet replace `GetStats()` internally.
