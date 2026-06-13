# Observability

StateForge includes telemetry counters, Prometheus output, and snapshot-backed metrics.

## Telemetry

Tracked areas include:

- reads
- writes
- deletes
- lock acquisitions
- lock contentions
- cleanups
- quarantines
- corruptions

## Prometheus

Prometheus text output is provided by `StateForge.Prometheus`.

Validation:

```powershell
.\scripts\Test-StateForgeObservability.ps1
```

## Snapshot-Backed Metrics

Snapshot-backed metrics allow the system to expose operational state without constantly scanning the store.
