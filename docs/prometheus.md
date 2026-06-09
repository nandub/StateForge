# Prometheus

StateForge v0.16.2 adds Prometheus text exposition support.

## Kestrel Harness Endpoint

```text
GET /stateforge/prometheus
```

## CLI Output

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- prometheus `
    --root D:\StateForge
```
