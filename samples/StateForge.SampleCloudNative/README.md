# Cloud-Native Minimal API Sample

This ASP.NET Core 8 minimal API demonstrates StateForge's environment-driven cache registration,
Kubernetes-style health endpoints, and in-process telemetry endpoints.

## Prerequisites

- .NET 8 SDK
- Write access to the configured root
- PowerShell, `curl`, or another HTTP client

## Run Locally

```powershell
dotnet run --project .\samples\StateForge.SampleCloudNative -- --urls http://localhost:5081
```

For local execution, the sample sets `STATEFORGE_ROOT_PATH` to an `App_Data\StateForge` directory when
the variable is absent.

Exercise the cache:

```powershell
Invoke-RestMethod -Method Put `
  -Uri http://localhost:5081/cache/example `
  -ContentType application/json `
  -Body '{"value":"hello"}'

Invoke-RestMethod http://localhost:5081/cache/example
Invoke-RestMethod http://localhost:5081/healthz
Invoke-RestMethod http://localhost:5081/stateforge/metrics
Invoke-RestMethod -Method Delete http://localhost:5081/cache/example
```

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| `PUT` | `/cache/{key}` | Store UTF-8 JSON text with a 20-minute sliding expiration |
| `GET` | `/cache/{key}` | Read a cached value |
| `DELETE` | `/cache/{key}` | Remove a cached value |
| `GET` | `/livez` | Process liveness |
| `GET` | `/readyz` | Read/write/lock/enumerate/cleanup readiness |
| `GET` | `/healthz` | Detailed health, diagnostics, and store statistics |
| `GET` | `/stateforge/metrics` | In-process operation counters |
| `POST` | `/stateforge/metrics/reset` | Reset sample telemetry counters |

## Environment Configuration

`AddStateForgeCloudNativeCache` understands:

| Variable | Meaning |
|---|---|
| `STATEFORGE_ROOT_PATH` | Persistent store root |
| `STATEFORGE_COMPRESSION` | Enable or disable compression |
| `STATEFORGE_ENCRYPTION` | Enable encryption |
| `STATEFORGE_PROTECTION_MODE` | `none`, `aes`, or `dpapi` |
| `STATEFORGE_AES_KEY_BASE64` | AES key; supplying it enables AES |
| `STATEFORGE_KEEP_BACKUPS` | Keep replacement backups |
| `STATEFORGE_SHARD_DEPTH` | Shard depth from 0 through 2 |
| `STATEFORGE_MUTEX_TIMEOUT_MS` | Per-key mutex timeout |

Example:

```powershell
$env:STATEFORGE_ROOT_PATH = "C:\StateForge\CloudNativeSample"
$env:STATEFORGE_COMPRESSION = "true"
$env:STATEFORGE_SHARD_DEPTH = "1"
$env:STATEFORGE_AES_KEY_BASE64 = "BASE64_ENCODED_AES_KEY"
dotnet run --project .\samples\StateForge.SampleCloudNative -- --urls http://localhost:5081
```

## Container and Kubernetes Use

Mount persistent storage at the configured root and inject AES material through a runtime secret. Use
`/livez` for liveness and `/readyz` for readiness. Restrict `/healthz`, telemetry, and cache demonstration
endpoints at the ingress or remove them from production applications.

The repository's `Dockerfile` and `deploy\k8s` manifests provide the production-oriented container
baseline. This sample focuses on application registration and endpoint behavior.
