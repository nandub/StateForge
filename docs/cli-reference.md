# CLI Reference

StateForge.Tools provides diagnostics, inspection, cleanup, health checks, and key generation.

## Generate AES Key

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- generate-key
```

## Diagnostics

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- diag --root D:\StateForge
```

## List Sessions

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- list --root D:\StateForge --format json
```

## List AES Sessions

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- list `
    --root D:\StateForge `
    --format json `
    --protection aes `
    --aes-key "<base64-key>"
```

## Stats

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- stats --root D:\StateForge --format json
```

## Validate

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- validate --root D:\StateForge
```

## Health

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- health --root D:\StateForge
```

## Cleanup

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- cleanup --root D:\StateForge
```

## Remove Session

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- remove --root D:\StateForge --key SESSIONKEY
```
