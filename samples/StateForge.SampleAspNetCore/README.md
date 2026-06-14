# ASP.NET Core Session Sample

This ASP.NET Core 8 application registers StateForge as `IDistributedCache`, enables ASP.NET Core
session middleware, and stores a counter in `HttpContext.Session`.

It is the recommended starting point for applications that already use ASP.NET Core session state.

## Prerequisites

- .NET 8 SDK
- Write access to the StateForge root
- A browser or HTTP client

## Run

From the repository root:

```powershell
dotnet run --project .\samples\StateForge.SampleAspNetCore -- --urls http://localhost:5080
```

Open:

```text
http://localhost:5080/
```

Refresh the page. The counter increases while the session cookie remains valid. Stop and restart the
application, then refresh again to verify that the session persisted on disk.

The default root is `App_Data\StateForge` under the sample content root. Override it before startup:

```powershell
$env:STATEFORGE_ROOT_PATH = "C:\StateForge\SessionSample"
dotnet run --project .\samples\StateForge.SampleAspNetCore -- --urls http://localhost:5080
```

The session cookie is protected with ASP.NET Core Data Protection. The sample persists those keys under
`App_Data\DataProtectionKeys`; override that separate path with
`STATEFORGE_DATA_PROTECTION_PATH`. Do not put Data Protection keys inside the StateForge session root.

## Integration Details

`AddStateForgeDistributedCache` registers:

- `StateForgeDistributedCacheOptions`
- `StateForgeFileStoreOptions`
- `IStateForgeStore`
- `IDistributedCache`

`AddSession` and `UseSession` then use that cache for ASP.NET Core session data. The sample sets an
HTTP-only, essential session cookie and a 20-minute idle timeout.

StateForge uses:

- one-level sharding
- compression
- no backups
- no encryption unless an AES key is supplied

## Enable AES

```powershell
$env:STATEFORGE_ROOT_PATH = "C:\StateForge\SessionSample"
$env:STATEFORGE_AES_KEY_BASE64 = "BASE64_ENCODED_AES_KEY"
dotnet run --project .\samples\StateForge.SampleAspNetCore -- --urls http://localhost:5080
```

When the key is present, the sample selects `StateForgeProtectionMode.Aes`. Every application instance
sharing the root must use the same key. Rotating or removing the key without migrating existing records
makes those sessions unreadable.

## Scale-Out

For multiple application instances:

1. Point every instance at the same shared root.
2. Use identical shard, compression, expiration, and AES settings.
3. Ensure the storage supports the locking and atomic file operations StateForge requires.
4. Set `STATEFORGE_DATA_PROTECTION_PATH` to a separately protected shared key directory when session
   cookies must work across instances.

StateForge persists session payloads; it does not replace ASP.NET Core data-protection key management.

## Production Notes

- Use HTTPS and configure the session cookie's `SecurePolicy`.
- Restrict filesystem permissions to application and operator identities.
- Protect and back up Data Protection keys separately from StateForge session records.
- Keep AES keys in a secret manager or protected environment injection.
- Add StateForge health, metrics, cleanup, and capacity monitoring.
- Do not place the store under the web root.
