# Performance Baselines

This directory contains reviewed, machine-readable StateForge reference results. It is intentionally
tracked because validation must work from a clean clone. Generated candidate reports belong under
`artifacts\performance` and remain ignored.

Profiles:

| Profile | Sessions | Payload | Threads |
|---|---:|---:|---:|
| small | 250 | 512 bytes | 2 |
| medium | 1,000 | 1,024 bytes | 4 |
| large | 3,000 | 4,096 bytes | 8 |

Each profile measures concurrent create, read, lock/update, and refresh operations; store statistics;
Prometheus collection; cleanup; full replication; and full snapshot creation. JSON reports also record
store bytes and managed-memory growth.

Regenerate only after reviewing the environment and results:

```powershell
.\scripts\Invoke-StateForgePerformanceBaseline.ps1 -Profile All -UpdateBaseline
```

The automated gate runs the small profile and uses broad relative thresholds. It is a regression signal,
not a cross-machine performance guarantee.
