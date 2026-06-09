# Getting Started

## Step 1: Open the Repository

```powershell
cd C:\Users\ferna\development\code\dotnet\StateForge
```

## Step 2: Repair and Validate the Solution

```powershell
.\scripts\Repair-StateForgeSolution.ps1
.\scripts\Test-StateForgeLayout.ps1
.\scripts\Test-StateForgeSource.ps1
.\scripts\Test-StateForgeSolution.ps1
```

## Step 3: Build

```powershell
.\scripts\Build-StateForge.ps1
```

Expected:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
```

## Step 4: Run Smoke Tests

```powershell
.\scripts\Invoke-StateForgeSmokeTest.ps1 -RootPath ..\StateForgeSmoke -Keep
```

## Step 5: Inspect the Demo Store

```powershell
.\scripts\Show-StateForgeSmokeDemo.ps1 -RootPath ..\StateForgeSmoke
```

## Step 6: Validate Classic ASP.NET Without IIS

```powershell
.\scripts\Invoke-StateForgeAspNetHarness.ps1 -RootPath ..\StateForgeAspNetHarness -Keep
```

## Step 7: Validate ASP.NET Core With Kestrel

Terminal 1:

```powershell
.\scripts\Start-StateForgeKestrelHarness.ps1 -RootPath ..\StateForgeKestrel -Url http://localhost:5075
```

Terminal 2:

```powershell
.\scripts\Test-StateForgeKestrelHarness.ps1 -Url http://localhost:5075
```

## Step 8: Continue

Read:

- `docs/configuration.md`
- `docs/aspnet-provider.md`
- `docs/aspnetcore-provider.md`
- `docs/production-deployment.md`

## Telemetry

See `docs/telemetry.md` for EventSource, metric snapshots, CLI metrics, and ASP.NET Core telemetry endpoints.

## Key Ring

Create a development key ring:

```powershell
.\scripts\New-StateForgeKeyRing.ps1 -OutFile .\stateforge-keyring.json -KeyId key-001
```

See `docs/key-rotation.md`.

## Maintenance Test

```powershell
.\scripts\Invoke-StateForgeMaintenance.ps1 -RootPath ..\StateForgeSmoke\demo -Once all
```

## STFG2 Envelope Test

```powershell
.\scripts\Test-StateForgeStfg2Envelope.ps1
```

## STFG2 Migration Test

```powershell
.\scripts\Test-StateForgeStfg2Migration.ps1
```

## STFG2 Store Migration Test

```powershell
.\scripts\Test-StateForgeStfg2StoreMigration.ps1
```

## Maintenance Host Test

```powershell
.\scripts\Test-StateForgeMaintenanceHost.ps1
```

## Release Readiness

```powershell
.\scripts\Test-StateForgeRelease.ps1 -PackageOutputPath .\artifacts\nuget
```

## Scale Validation

```powershell
.\scripts\Test-StateForgeScale.ps1
```

## API Validation

```powershell
.\scripts\Test-StateForgeApiValidation.ps1
```

## Performance Helper Validation

```powershell
.\scripts\Test-StateForgePerformance.ps1
.\scripts\Test-StateForgeSharding.ps1
```

## Snapshot-Backed Metrics Validation

```powershell
.\scripts\Test-StateForgeSnapshotMetrics.ps1
```
