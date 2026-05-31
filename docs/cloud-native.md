# Cloud-Native Usage

StateForge can run in cloud-native environments when storage is durable, configuration is externalized, and health checks are wired into the platform.

## Core Rule

Do not use pod-local or container-local disk for production session state.

Use durable storage:

- Kubernetes PersistentVolumeClaim
- Azure Files
- AWS EFS
- Amazon FSx
- SMB share

For highly elastic production systems, a managed distributed cache such as Redis is usually more cloud-native than a shared file store.

## Environment Variables

| Variable | Purpose |
|---|---|
| `STATEFORGE_ROOT_PATH` | Root storage path |
| `STATEFORGE_COMPRESSION` | `true` or `false` |
| `STATEFORGE_ENCRYPTION` | `true` or `false` |
| `STATEFORGE_PROTECTION_MODE` | `none`, `dpapi`, or `aes` |
| `STATEFORGE_AES_KEY_BASE64` | AES key |
| `STATEFORGE_KEEP_BACKUPS` | `true` or `false` |
| `STATEFORGE_SHARD_DEPTH` | Sharding depth |
| `STATEFORGE_STALE_LOCK_MINUTES` | Stale lock timeout |
| `STATEFORGE_MUTEX_TIMEOUT_MS` | Mutex timeout |

## ASP.NET Core Setup

```csharp
using StateForge.CloudNative;

builder.Services.AddStateForgeCloudNativeCache();

WebApplication app = builder.Build();

app.MapStateForgeCloudNativeHealth();
```

## Health Endpoints

| Endpoint | Purpose |
|---|---|
| `/livez` | Process liveness |
| `/readyz` | Read/write/lock readiness |
| `/healthz` | Full health, diagnostics, and stats |

## Docker

```powershell
docker build -t stateforge-kestrel:0.9.0 .
```

```powershell
docker run --rm -p 8080:8080 `
    -e STATEFORGE_ROOT_PATH=/data/stateforge `
    -e STATEFORGE_COMPRESSION=true `
    -v stateforge-data:/data/stateforge `
    stateforge-kestrel:0.9.0
```

Test:

```powershell
Invoke-RestMethod http://localhost:8080/livez
Invoke-RestMethod http://localhost:8080/readyz
Invoke-RestMethod http://localhost:8080/healthz
```

## Kubernetes

Manifests are under:

```text
deploy/k8s/
```

Apply:

```powershell
kubectl apply -f deploy/k8s/pvc.yaml
kubectl apply -f deploy/k8s/configmap.yaml
kubectl apply -f deploy/k8s/secret.yaml
kubectl apply -f deploy/k8s/deployment.yaml
kubectl apply -f deploy/k8s/service.yaml
kubectl apply -f deploy/k8s/hpa.yaml
```

## Security

Do not commit production AES keys. Store keys in:

- Kubernetes Secret
- Azure Key Vault
- AWS Secrets Manager
- HashiCorp Vault

## Telemetry Endpoints

When `StateForge.Telemetry.AspNetCore` is enabled, use:

```text
/stateforge/metrics
/stateforge/metrics/reset
```
