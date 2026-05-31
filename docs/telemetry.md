# Telemetry

StateForge v0.10.0 adds a first observability layer.

## Projects

| Project | Purpose |
|---|---|
| `StateForge.Telemetry` | EventSource, DiagnosticSource, and in-process metrics |
| `StateForge.Telemetry.AspNetCore` | ASP.NET Core metrics endpoints |

## Metrics

Current counters:

- reads
- writes
- deletes
- locks acquired
- lock contentions
- cleanups
- quarantines
- corruptions

## ASP.NET Core Registration

```csharp
using StateForge.Telemetry.AspNetCore;

builder.Services.AddStateForgeTelemetry();

WebApplication app = builder.Build();

app.MapStateForgeTelemetry();
```

## HTTP Endpoints

| Endpoint | Purpose |
|---|---|
| `GET /stateforge/metrics` | Returns current in-process metric snapshot |
| `POST /stateforge/metrics/reset` | Resets in-process counters |

## CLI

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- metrics
```

JSON:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- metrics --format json
```

## EventSource

Provider name:

```text
StateForge
```

Initial events:

```text
SessionRead
SessionWritten
SessionDeleted
LockAcquired
LockContention
CleanupCompleted
FileQuarantined
CorruptionDetected
HealthCheckFailed
HealthCheckPassed
```

## Notes

This release adds the telemetry foundation. The next step is to wire metric recording directly into FileStore operations and add OpenTelemetry Meter support.


## v0.10.1 Runtime Metrics

StateForge v0.10.1 records metrics from the Kestrel validation harness.

Covered runtime paths:

- `/health` records write/read activity
- `POST /session/{id}/{value}` records write activity
- `GET /session/{id}` records read activity
- `DELETE /session/{id}` records delete activity

Validate with:

```powershell
.\scripts\Test-StateForgeTelemetry.ps1 -Url http://localhost:5075
```

Expected:

```text
Reads   > 0
Writes  > 0
Deletes > 0
Success = True
```

The `StateForgeTelemetryScope` helper was added so future FileStore instrumentation can record metrics safely without allowing telemetry failures to affect session operations.
