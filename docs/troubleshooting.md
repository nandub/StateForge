# Troubleshooting

## Build Fails Because DLL Is Locked

Symptom:

```text
The file is locked by: StateForge.KestrelHarness
```

Cause:

The Kestrel harness is still running and has loaded StateForge assemblies.

Fix:

```powershell
Get-Process StateForge.KestrelHarness -ErrorAction SilentlyContinue | Stop-Process -Force
.\scripts\Build-StateForge.ps1
```

## Duplicate Projects in Solution

Fix:

```powershell
.\scripts\Repair-StateForgeSolution.ps1
.\scripts\Test-StateForgeSolution.ps1
```

## AES Records Not Listed

If diagnostics shows more files than `list` or `stats`, the tool probably needs the AES key.

Use:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- list `
    --root D:\StateForge `
    --format json `
    --protection aes `
    --aes-key "<base64-key>"
```

## ASP.NET Provider Fails in IIS

Check:

- Assemblies are in `bin`
- App pool identity can modify `RootPath`
- `Load User Profile=True` if using DPAPI
- `web.config` provider attributes are valid

## High Backup Count

Set:

```text
keepBackups=false
```

Backups can significantly slow update-heavy workloads.


## Solution Maintenance

If new projects are added and the solution has duplicate entries, regenerate the solution:

```powershell
.\scripts\Repair-StateForgeSolution.ps1
.\scripts\Test-StateForgeSolution.ps1
```

## Kestrel Harness DLL Locks

If `Build-StateForge.ps1` fails because `StateForge.KestrelHarness` has locked DLLs, stop the harness:

```powershell
Get-Process StateForge.KestrelHarness -ErrorAction SilentlyContinue | Stop-Process -Force
```

Then rebuild:

```powershell
.\scripts\Build-StateForge.ps1
```
