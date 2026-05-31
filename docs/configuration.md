# Configuration

## Step 1: Pick a RootPath

Single server:

```text
D:\StateForge
```

Farm:

```text
\\FileServer\StateForge
```

## Step 2: Set Permissions

```powershell
New-Item -Path D:\StateForge -ItemType Directory -Force
icacls D:\StateForge /grant "DOMAIN\svc-web:(OI)(CI)M"
```

## Step 3: Choose Defaults

Recommended:

```text
EnableCompression=true
KeepBackups=false
ShardDepth=1
StaleLockMinutes=5
MutexTimeoutMilliseconds=30000
```

## Step 4: Choose Encryption

| Scenario | Mode |
|---|---|
| Development | None |
| Single server | None or DPAPI |
| Web farm | AES |
| Shared SMB storage | AES |

## Step 5: Validate

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- validate --root D:\StateForge
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- health --root D:\StateForge
```
