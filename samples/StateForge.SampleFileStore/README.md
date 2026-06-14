# Direct FileStore Sample

This console application demonstrates the lowest-level supported integration: constructing
`StateForgeFileStoreOptions` and using `StateForgeFileStore` directly.

Use this approach for workers, console applications, services, custom adapters, and applications that do
not use ASP.NET session or `IDistributedCache`.

## Prerequisites

- .NET 8 SDK
- Write access to the selected store directory
- The StateForge repository checkout, because the sample uses project references

## Run the Demo

From the repository root:

```powershell
dotnet run --project .\samples\StateForge.SampleFileStore -- demo
```

The default store is:

```text
samples\StateForge.SampleFileStore\bin\Debug\net8.0\App_Data\StateForge
```

Each run reads `sample:counter`, increments it, and writes it back with a 20-minute expiration.

Use a specific store root:

```powershell
dotnet run --project .\samples\StateForge.SampleFileStore -- demo --root C:\StateForgeDemo
```

## Commands

```powershell
dotnet run --project .\samples\StateForge.SampleFileStore -- set greeting hello --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- get greeting --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- list --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- stats --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- remove greeting --root C:\StateForgeDemo
```

`set` stores UTF-8 text for 20 minutes. `list` displays expiration, payload size, compression, and AES
metadata. `stats` shows aggregate session and payload counts.

## Configuration

The sample configures:

- `ShardDepth = 1`
- compression enabled
- backups disabled
- a local `App_Data\StateForge` root when `--root` is omitted
- AES only when `STATEFORGE_AES_KEY_BASE64` is present

Before using the store, the sample calls `ValidateConfiguration()` and prints configuration errors.

## Enable AES

Set a Base64-encoded AES key before starting the application:

```powershell
$env:STATEFORGE_AES_KEY_BASE64 = "BASE64_ENCODED_AES_KEY"
dotnet run --project .\samples\StateForge.SampleFileStore -- set secret value --root C:\StateForgeDemo
dotnet run --project .\samples\StateForge.SampleFileStore -- list --root C:\StateForgeDemo
```

New AES records are authenticated. The same key must be available for every process that reads the
store. Keep the key outside the store root, snapshots, logs, and source control.

## Production Notes

- Put the root on storage with the required durability and sharing semantics.
- Grant write access only to the service identity and StateForge operators.
- Use the same shard depth, compression, and encryption settings on all writers.
- Do not expose `PhysicalPath` values to untrusted clients.
- Run `CheckHealth()` and monitor cleanup, corruption, and storage capacity in the hosting service.
