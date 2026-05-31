# Farm Mode

StateForge can support web farm scenarios when all nodes share the same session storage path and encryption configuration.

## Shared Storage

Example:

```text
\\FileServer\StateForge
```

or a shared SAN/cluster volume.

## Requirements

- All nodes must use the same `RootPath`.
- All nodes must use the same `ShardDepth`.
- All nodes must use the same AES key when AES is enabled.
- Application pool identities need read/write/modify rights to the shared folder.

## Recommended Farm Configuration

```text
EnableCompression=true
EnableEncryption=true
ProtectionMode=Aes
KeepBackups=false
ShardDepth=1
```

## Farm Simulation

```powershell
$key = dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- generate-key

.\scripts\Invoke-StateForgeFarmTest.ps1 `
    -RootPath ..\StateForgeFarm `
    -AesKeyBase64 $key `
    -Keep
```

Expected scenario:

```text
NodeA writes
NodeB reads
NodeC locks and updates
NodeD reads updated value
```
