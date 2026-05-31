# Testing

## Recommended Test Order

```powershell
.\scripts\Repair-StateForgeSolution.ps1
.\scripts\Test-StateForgeLayout.ps1
.\scripts\Test-StateForgeSource.ps1
.\scripts\Test-StateForgeSolution.ps1
.\scripts\Build-StateForge.ps1
```

## Smoke Test

```powershell
.\scripts\Invoke-StateForgeSmokeTest.ps1 -RootPath ..\StateForgeSmoke -Keep
```

## Demo Inspection

```powershell
.\scripts\Show-StateForgeSmokeDemo.ps1 -RootPath ..\StateForgeSmoke
```

## ASP.NET Provider Harness

```powershell
.\scripts\Invoke-StateForgeAspNetHarness.ps1 -RootPath ..\StateForgeAspNetHarness -Keep
```

## Kestrel Harness

Terminal 1:

```powershell
.\scripts\Start-StateForgeKestrelHarness.ps1 -RootPath ..\StateForgeKestrel -Url http://localhost:5075
```

Terminal 2:

```powershell
.\scripts\Test-StateForgeKestrelHarness.ps1 -Url http://localhost:5075
```

## Farm Test

```powershell
$key = dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- generate-key
.\scripts\Invoke-StateForgeFarmTest.ps1 -RootPath ..\StateForgeFarm -AesKeyBase64 $key -Keep
```

## Resilience Test

```powershell
.\scripts\Invoke-StateForgeResilienceTest.ps1 -RootPath ..\StateForgeResilience -Sessions 10000 -Keep
```
