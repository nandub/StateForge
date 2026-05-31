# Benchmarking

## Basic Benchmark

```powershell
.\scripts\Invoke-StateForgeBenchmark.ps1
```

## Larger Benchmark

```powershell
.\scripts\Invoke-StateForgeBenchmark.ps1 `
    -RootPath ..\StateForgeBench `
    -Sessions 10000 `
    -PayloadBytes 4096 `
    -Threads 8 `
    -Compression `
    -Keep
```

## AES Benchmark

```powershell
$key = dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- generate-key

.\scripts\Invoke-StateForgeBenchmark.ps1 `
    -RootPath ..\StateForgeBenchAes `
    -Sessions 10000 `
    -PayloadBytes 4096 `
    -Threads 8 `
    -Compression `
    -Aes `
    -AesKeyBase64 $key `
    -Keep
```

## Backup Comparison

Backups are disabled by default in benchmark mode.

Enable backups only when intentionally measuring backup overhead:

```powershell
.\scripts\Invoke-StateForgeBenchmark.ps1 `
    -RootPath ..\StateForgeBenchBackups `
    -Sessions 10000 `
    -PayloadBytes 4096 `
    -Threads 8 `
    -Compression `
    -Keep `
    -KeepBackups
```
