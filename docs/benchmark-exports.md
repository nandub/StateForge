# Benchmark Exports

StateForge v0.18.0 adds benchmark export support.

## JSON Export

```powershell
.\scripts\Invoke-StateForgeScaleTest.ps1 `
    -Sessions 25000 `
    -ExportJson .\artifacts\benchmarks\scale.json
```

## CSV Export

```powershell
.\scripts\Invoke-StateForgeScaleTest.ps1 `
    -Sessions 25000 `
    -ExportCsv .\artifacts\benchmarks\scale.csv
```

## Fields

| Field | Description |
|---|---|
| name | Scenario name |
| operations | Number of logical operations |
| elapsedMs | Total elapsed milliseconds |
| opsPerSecond | Operations per second |
| p50Ms | 50th percentile latency |
| p95Ms | 95th percentile latency |
| p99Ms | 99th percentile latency |

## Compare

```powershell
.\scripts\Compare-StateForgeBenchmark.ps1 `
    -BaselineCsv .\artifacts\benchmarks\old.csv `
    -CandidateCsv .\artifacts\benchmarks\new.csv
```
