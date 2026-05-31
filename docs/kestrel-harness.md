# Kestrel Harness

## Step 1: Build

```powershell
.\scripts\Build-StateForge.ps1
```

## Step 2: Start Harness

```powershell
.\scripts\Start-StateForgeKestrelHarness.ps1 -RootPath ..\StateForgeKestrel -Url http://localhost:5075
```

## Step 3: Test Harness

In a second terminal:

```powershell
.\scripts\Test-StateForgeKestrelHarness.ps1 -Url http://localhost:5075
```

Expected:

```text
PASS: Kestrel health
PASS: Kestrel set
PASS: Kestrel get
PASS: Kestrel delete
```

## Step 4: Manual HTTP Checks

```powershell
Invoke-RestMethod http://localhost:5075/health
Invoke-RestMethod -Method Post -Uri http://localhost:5075/session/demo/hello
Invoke-RestMethod http://localhost:5075/session/demo
Invoke-RestMethod -Method Delete -Uri http://localhost:5075/session/demo
```

## Step 5: Stop Harness

Press `Ctrl+C`.

If DLLs are locked:

```powershell
Get-Process StateForge.KestrelHarness -ErrorAction SilentlyContinue | Stop-Process -Force
```
