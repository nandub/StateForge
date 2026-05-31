# Encryption

StateForge supports three protection modes.

## None

Fastest mode.

Use this when:

- The server is trusted.
- The volume is encrypted.
- File ACLs are sufficient.

## DPAPI

Windows DPAPI protection.

Best for:

- Single-server deployments
- No shared session store
- No key management requirement

Limitations:

- Machine/user scoped
- Not ideal for web farms

## AES

AES protection with a shared key.

Best for:

- Web farms
- Shared SMB storage
- Multiple servers reading the same sessions

Generate a key:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- generate-key
```

Inspect AES records:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- list `
    --root D:\StateForge `
    --format json `
    --protection aes `
    --aes-key "<base64-key>"
```

## Recommendation

| Deployment | Recommended Mode |
|---|---|
| Development | None |
| Single IIS server | DPAPI or None |
| IIS farm | AES |
| Shared network storage | AES |
