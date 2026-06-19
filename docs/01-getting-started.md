# Getting Started

This guide is for this repository: `nandub/StateForge`, a resilient, file-backed session and state
platform for .NET applications. It is not the unrelated finite-state-machine project with a similar
name.

StateForge stores session, cache, and state records as files on disk instead of keeping them only in
memory or requiring Redis, SQL Server, or another external cache service.

Current repository version: **1.0.0**

## Choose an Integration

| Scenario | Use this component |
|---|---|
| Console app, worker, Windows service, or custom state adapter | `StateForge.FileStore` |
| ASP.NET Core session or `IDistributedCache` | `StateForge.AspNetCore` |
| ASP.NET Framework Web Forms or MVC 5 session state | `StateForge.AspNet` |
| Cloud-native app with environment configuration, health checks, and telemetry | `StateForge.CloudNative` |

The maintained examples are listed in `samples\README.md`; each sample folder has its own detailed
`README.md`.

## Prerequisites

For the easiest path, install:

```powershell
git --version
dotnet --info
```

You need:

- .NET 8 SDK
- Git
- PowerShell
- a writable folder for StateForge data

For ASP.NET Framework integration, you also need Windows, the .NET Framework 4.8.1 Developer Pack,
Visual Studio with the ASP.NET and web development workload, and IIS Express or IIS.

## Clone, Build, and Validate

```powershell
git clone https://github.com/nandub/StateForge.git
cd StateForge
.\scripts\Build-StateForge.ps1
```

Run the basic validation checks:

```powershell
.\scripts\Test-StateForgeDocs.ps1
.\scripts\Test-StateForgeVersionConsistency.ps1
.\scripts\Test-StateForgeIncrementalSnapshots.ps1
```

Run broader validation when preparing a change or release:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Samples
.\scripts\Test-StateForge.ps1 -Suite Security
.\scripts\Test-StateForge.ps1 -Suite Production
```

The production suite covers non-interactive validation, docs, version consistency, layout/source guards,
API docs, API compatibility, upgrade compatibility, security, samples, health checks, smoke tests,
observability, replication, replica catch-up, replica monitoring, quorum, witness checks, split-brain
guards, multi-site behavior, Docker/Kubernetes deployment assets, package metadata, performance,
snapshots, and recovery flow.

## Run the Simplest FileStore Demo

The direct FileStore sample is the best first test because it uses `StateForgeFileStore` without any web
framework:

```powershell
dotnet run --project .\samples\StateForge.SampleFileStore -- demo
```

By default, the sample stores data under:

```text
samples\StateForge.SampleFileStore\bin\Debug\net8.0\App_Data\StateForge
```

Each run reads `sample:counter`, increments it, and writes it back with a 20-minute expiration.

Use a specific storage folder:

```powershell
dotnet run --project .\samples\StateForge.SampleFileStore -- demo --root C:\StateForgeDemo
```

Try the basic commands:

```powershell
dotnet run --project .\samples\StateForge.SampleFileStore -- set greeting hello --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- get greeting --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- list --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- stats --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- remove greeting --root C:\StateForgeDemo
```

The sample uses one-level sharding, compression enabled, backups disabled, a local `App_Data\StateForge`
root when `--root` is omitted, and AES only when `STATEFORGE_AES_KEY_BASE64` is present.

## Direct FileStore Usage

Reference the StateForge projects during development or use locally built NuGet packages. The shipped
package set includes `StateForge.Core`, `StateForge.FileStore`, `StateForge.AspNetCore`,
`StateForge.AspNet`, `StateForge.Security`, `StateForge.Telemetry`, and related operational packages.

A minimal direct FileStore example:

```csharp
using System.Text;
using StateForge.Core;
using StateForge.FileStore;

var options = new StateForgeFileStoreOptions
{
    RootPath = Path.GetFullPath(@"C:\StateForgeDemo"),
    EnableCompression = true,
    KeepBackups = false,
    ShardDepth = 1
};

var store = new StateForgeFileStore(options);

var validation = store.ValidateConfiguration();
if (!validation.Success)
{
    foreach (var error in validation.Errors)
    {
        Console.Error.WriteLine(error);
    }

    return;
}

store.Set(
    key: "demo:greeting",
    value: Encoding.UTF8.GetBytes("hello"),
    expiresIn: TimeSpan.FromMinutes(20));

var entry = store.Get("demo:greeting");

Console.WriteLine(entry is null
    ? "Not found"
    : Encoding.UTF8.GetString(entry.Value));
```

This mirrors `samples\StateForge.SampleFileStore\Program.cs`: construct options, validate the
configuration, then call `Set`, `Get`, `Remove`, `List`, or `GetStats`.

## Enable AES Encryption

Generate a development AES key:

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$env:STATEFORGE_AES_KEY_BASE64 = [Convert]::ToBase64String($bytes)
```

Run the sample with the key present:

```powershell
dotnet run --project .\samples\StateForge.SampleFileStore -- set secret value --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- list --root C:\StateForgeDemo
```

The same AES key must be available to every process that reads the store. Do not commit the key or store
it inside the StateForge data root, snapshots, or logs.

In code:

```csharp
var aesKey = Environment.GetEnvironmentVariable("STATEFORGE_AES_KEY_BASE64");

var options = new StateForgeFileStoreOptions
{
    RootPath = Path.GetFullPath(@"C:\StateForgeDemo"),
    EnableCompression = true,
    KeepBackups = false,
    ShardDepth = 1
};

if (!string.IsNullOrWhiteSpace(aesKey))
{
    options.EnableEncryption = true;
    options.ProtectionMode = StateForgeProtectionMode.Aes;
    options.AesKeyBase64 = aesKey;
}
```

New AES records are authenticated. Encryption protects record contents, but it does not replace
least-privilege filesystem permissions.

## Configure ASP.NET Core Session State

Use this path when your application already uses `HttpContext.Session`.

Example `Program.cs`:

```csharp
using Microsoft.AspNetCore.DataProtection;
using StateForge.AspNetCore;
using StateForge.Core;

var builder = WebApplication.CreateBuilder(args);

var rootPath = Environment.GetEnvironmentVariable("STATEFORGE_ROOT_PATH")
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "StateForge");

var dataProtectionPath = Environment.GetEnvironmentVariable("STATEFORGE_DATA_PROTECTION_PATH")
    ?? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");

var aesKey = Environment.GetEnvironmentVariable("STATEFORGE_AES_KEY_BASE64");

Directory.CreateDirectory(dataProtectionPath);

builder.Services
    .AddDataProtection()
    .SetApplicationName("MyStateForgeApp")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

builder.Services.AddStateForgeDistributedCache(options =>
{
    options.RootPath = Path.GetFullPath(rootPath);
    options.StaleLockMinutes = 5;
    options.DefaultExpirationMinutes = 20;
    options.ShardDepth = 1;
    options.EnableCompression = true;
    options.KeepBackups = false;

    if (!string.IsNullOrWhiteSpace(aesKey))
    {
        options.EnableEncryption = true;
        options.ProtectionMode = StateForgeProtectionMode.Aes;
        options.AesKeyBase64 = aesKey;
    }
});

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(20);
});

var app = builder.Build();

app.UseSession();

app.MapGet("/", context =>
{
    var value = context.Session.GetInt32("Counter") ?? 0;
    value++;

    context.Session.SetInt32("Counter", value);

    return context.Response.WriteAsync($"StateForge session counter: {value}");
});

app.Run();
```

Run the repository sample:

```powershell
$env:STATEFORGE_ROOT_PATH = "C:\StateForge\SessionSample"
$env:STATEFORGE_DATA_PROTECTION_PATH = "C:\StateForge\DataProtectionKeys"

dotnet run --project .\samples\StateForge.SampleAspNetCore -- --urls http://localhost:5080
```

Open:

```text
http://localhost:5080/
```

Refresh the page. Stop and restart the app, then refresh again. The counter should persist on disk.

Keep ASP.NET Core Data Protection keys separate from StateForge session records. StateForge stores
session payloads; it does not replace ASP.NET Core Data Protection key management.

## Configure Cloud-Native Apps

Use the cloud-native sample when you want environment-variable configuration, health endpoints, and
telemetry.

Run locally:

```powershell
dotnet run --project .\samples\StateForge.SampleCloudNative -- --urls http://localhost:5081
```

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

Supported environment variables:

| Variable | Meaning |
|---|---|
| `STATEFORGE_ROOT_PATH` | Persistent store root |
| `STATEFORGE_COMPRESSION` | Enable or disable compression |
| `STATEFORGE_ENCRYPTION` | Enable encryption |
| `STATEFORGE_PROTECTION_MODE` | `none`, `aes`, or `dpapi` |
| `STATEFORGE_AES_KEY_BASE64` | AES key; supplying it enables AES |
| `STATEFORGE_KEEP_BACKUPS` | Keep replacement backups |
| `STATEFORGE_SHARD_DEPTH` | Shard depth from `0` through `2` |
| `STATEFORGE_MUTEX_TIMEOUT_MS` | Per-key mutex timeout |

Example:

```powershell
$env:STATEFORGE_ROOT_PATH = "C:\StateForge\CloudNativeSample"
$env:STATEFORGE_COMPRESSION = "true"
$env:STATEFORGE_SHARD_DEPTH = "1"
$env:STATEFORGE_AES_KEY_BASE64 = "BASE64_ENCODED_AES_KEY"

dotnet run --project .\samples\StateForge.SampleCloudNative -- --urls http://localhost:5081
```

For production, use `/livez` for liveness and `/readyz` for readiness. Restrict `/healthz`, telemetry,
and demo cache endpoints from public access or remove them from production applications.

## Configure ASP.NET Framework and IIS

Use this path only for classic ASP.NET Framework applications, such as Web Forms or MVC 5.

The sample targets .NET Framework 4.8.1 and IIS/IIS Express. It uses
`StateForgeSessionStateProvider` as a custom ASP.NET session-state provider.

Typical steps:

1. Create an **ASP.NET Web Application (.NET Framework)** targeting .NET Framework 4.8.1.
2. Choose Empty, Web Forms, or MVC 5.
3. Copy `Default.aspx` and `Web.config` from `samples\StateForge.SampleWebFramework`.
4. Reference `StateForge.AspNet.dll` and its StateForge dependencies, or reference
   `src\StateForge.AspNet\StateForge.AspNet.csproj` while developing.
5. Build and run under IIS Express or IIS.
6. Refresh the page and confirm that the session counter increments.
7. Restart the application and confirm that the counter persists.

When `rootPath` is omitted, the provider stores data under:

```text
<application-root>\App_Data\StateForge
```

Production deployments should use a path outside the web root:

```xml
rootPath="D:\StateForge\SessionStore"
```

Grant Modify permission only to the app-pool identity:

```powershell
.\scripts\Install-StateForgeStore.ps1 `
  -RootPath D:\StateForge\SessionStore `
  -Identity "IIS AppPool\MyApplicationPool"
```

Do not grant broad write permissions such as `Everyone` or `Users`.

## Recommended Folder Layout

Use separate folders for separate responsibilities:

```text
C:\StateForge\
  SessionStore\              # StateForge session/cache files
  DataProtectionKeys\        # ASP.NET Core Data Protection keys
  Snapshots\                 # Optional snapshots
  Logs\                      # App/platform logs, not secrets
```

Do not put the StateForge root under:

- `wwwroot`
- `public`
- static/content folders served by IIS or Kestrel
- the same folder as encryption keys

## Production Checklist

Before production, confirm:

```text
[ ] Root path is outside the web root.
[ ] Only the app identity and operators have filesystem access.
[ ] AES key is injected through a secret manager or protected environment variable.
[ ] Data Protection keys are stored separately from StateForge records.
[ ] All app instances use the same shard depth, compression, expiration, and AES settings.
[ ] Shared storage supports required locking and atomic file operations.
[ ] Health, metrics, cleanup, corruption, quarantine, and capacity are monitored.
[ ] Demo endpoints are removed or access-restricted.
[ ] Backups and snapshots are tested.
[ ] Production validation suite passes.
```

For ASP.NET Core scale-out, every instance should point to the same shared root, use identical
sharding/compression/expiration/AES settings, use storage that supports required locking and atomic file
operations, and share Data Protection keys separately when session cookies must work across instances.

## Docker and Kubernetes

Build the container:

```powershell
docker build --tag stateforge-kestrel:1.0.0 .
```

Run deployment validation:

```powershell
.\scripts\Test-StateForge.ps1 -Suite Deployment
```

The container runs non-root, stores data and snapshots under `/data/stateforge`, and disables demo
session endpoints.

For Kubernetes:

```powershell
kubectl apply -k .\deploy\k8s
```

The Kubernetes manifests require a `ReadWriteMany` storage class for multiple replicas, Metrics Server
for HPA, and pod security support for UID/GID `1654`. Encryption is disabled in the generic ConfigMap;
to enable AES, populate `stateforge-secret` with `STATEFORGE_AES_KEY_BASE64`, then set
`STATEFORGE_ENCRYPTION=true` and `STATEFORGE_PROTECTION_MODE=aes`.

## Troubleshooting

### Invalid StateForge Configuration

Run the sample with a simple writable path:

```powershell
mkdir C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- demo --root C:\StateForgeDemo
```

The direct sample calls `ValidateConfiguration()` and prints configuration errors before using the
store.

### Session Disappears After Restart

Check that:

- `STATEFORGE_ROOT_PATH` points to a persistent folder
- the app identity has write access
- you are not storing under a temp/build output folder
- Data Protection keys are persisted for ASP.NET Core

### Session Works on One Server but Not Another

Check that:

- all nodes use the same root path
- all nodes use the same AES key
- all nodes use the same shard depth
- ASP.NET Core Data Protection keys are shared separately
- classic ASP.NET `machineKey` is handled separately

StateForge stores session payloads. It does not replace ASP.NET Core Data Protection key management or
classic ASP.NET `machineKey` configuration.

### Encrypted Records Cannot Be Read

Check that:

- `STATEFORGE_AES_KEY_BASE64` is present
- the key is the same key used when records were written
- you did not rotate or remove the key without migrating old records

## Beginner Golden Path

For a first install, do this:

```powershell
git clone https://github.com/nandub/StateForge.git
cd StateForge

.\scripts\Build-StateForge.ps1
.\scripts\Test-StateForge.ps1 -Suite Samples

mkdir C:\StateForgeDemo

dotnet run --project .\samples\StateForge.SampleFileStore -- demo --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- set greeting hello --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- get greeting --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- stats --root C:\StateForgeDemo
```

Then move to ASP.NET Core:

```powershell
$env:STATEFORGE_ROOT_PATH = "C:\StateForge\SessionSample"
$env:STATEFORGE_DATA_PROTECTION_PATH = "C:\StateForge\DataProtectionKeys"

dotnet run --project .\samples\StateForge.SampleAspNetCore -- --urls http://localhost:5080
```

Open `http://localhost:5080/`, refresh, restart the app, refresh again, and confirm the session counter
survives the restart.

## Implementation References

This guide is consolidated from the current repository implementation and sample documentation:

- `samples\README.md`
- `samples\StateForge.SampleFileStore\README.md`
- `samples\StateForge.SampleFileStore\Program.cs`
- `samples\StateForge.SampleAspNetCore\README.md`
- `samples\StateForge.SampleCloudNative\README.md`
- `samples\StateForge.SampleWebFramework\README.md`
- `docs\12-production-readiness.md`
- `README-NUGET.md`
