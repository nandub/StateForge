# Production Deployment

## Storage Location

Recommended:

```text
D:\StateForge
```

For large environments, place StateForge on a dedicated data volume.

## Folder Layout

StateForge creates:

```text
D:\StateForge\sessions
D:\StateForge\temp
D:\StateForge\backups
D:\StateForge\quarantine
```

## Permissions

Grant modify rights to the application identity.

Example for IIS app pool:

```powershell
icacls D:\StateForge /grant "IIS AppPool\YourAppPool:(OI)(CI)M"
```

Example for service account:

```powershell
icacls D:\StateForge /grant "DOMAIN\svc-web:(OI)(CI)M"
```

## Antivirus Exclusions

Exclude:

```text
D:\StateForge\sessions
D:\StateForge\temp
```

Optional:

```text
D:\StateForge\backups
D:\StateForge\quarantine
```

## Recommended Production Settings

```text
enableCompression=true
keepBackups=false
shardDepth=1
staleLockMinutes=5
```

## Encryption Recommendation

| Scenario | Recommendation |
|---|---|
| Single server, disk encrypted | None |
| Single server, app-level encryption needed | DPAPI |
| Web farm | AES |
| Shared SMB storage | AES |

## Health Checks

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- health --root D:\StateForge
```

## Cleanup

Schedule cleanup during low-traffic periods:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- cleanup --root D:\StateForge
```

## Backup Strategy

StateForge session data is transient but operationally important.

Recommended:

- Do not back up `temp`
- Consider excluding `sessions` if session loss during disaster recovery is acceptable
- Back up configuration and AES keys securely
- Protect AES keys separately from session files

## Operational Checklist

Before production:

- Build passes
- Smoke tests pass
- ASP.NET harness passes
- Kestrel harness passes if ASP.NET Core is used
- Health check passes
- Folder permissions confirmed
- Antivirus exclusions configured
- AES key escrowed if AES is used
- Cleanup scheduled
