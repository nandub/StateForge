# StateForge Samples

These samples demonstrate the supported StateForge integration paths.

| Sample | Runtime | Use case |
|---|---|---|
| `StateForge.SampleFileStore` | .NET 8 console | Direct `StateForgeFileStore` API usage |
| `StateForge.SampleAspNetCore` | ASP.NET Core 8 | File-backed `IDistributedCache` and session state |
| `StateForge.SampleCloudNative` | ASP.NET Core 8 | Environment configuration, health endpoints, and telemetry |
| `StateForge.SampleWebFramework` | ASP.NET Framework 4.8.1 | Custom ASP.NET session-state provider under IIS |

Each folder contains a dedicated `README.md` with setup, execution, encryption, verification, and
deployment guidance.

## Repository Builds

The SDK-style samples use project references so they always compile against the current checkout:

```powershell
dotnet build .\samples\StateForge.SampleFileStore\StateForge.SampleFileStore.csproj -c Release
dotnet build .\samples\StateForge.SampleAspNetCore\StateForge.SampleAspNetCore.csproj -c Release
dotnet build .\samples\StateForge.SampleCloudNative\StateForge.SampleCloudNative.csproj -c Release
```

The ASP.NET Framework sample is a Web Forms deployment example. It requires IIS or IIS Express and the
ASP.NET 4.8.1 development workload; it is validated as configuration and markup rather than built by the
cross-platform .NET SDK.

## Encryption

The SDK-style samples enable authenticated AES records only when
`STATEFORGE_AES_KEY_BASE64` contains a valid Base64-encoded 128-, 192-, or 256-bit key.

Generate a development key:

```powershell
$bytes = New-Object byte[] 32
[System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$env:STATEFORGE_AES_KEY_BASE64 = [Convert]::ToBase64String($bytes)
```

Do not commit keys or store them inside the StateForge data root.

## Validation

Run the repository sample gate:

```powershell
.\scripts\Test-StateForgeSamples.ps1
```
