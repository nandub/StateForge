# Classic ASP.NET Session State Provider

StateForge.AspNet provides a custom ASP.NET Framework session-state provider.

## Assemblies

Copy the following assemblies to the web application's `bin` directory:

```text
StateForge.AspNet.dll
StateForge.FileStore.dll
StateForge.Core.dll
```

## Basic web.config

```xml
<configuration>
  <system.web>
    <sessionState
      mode="Custom"
      customProvider="StateForge"
      timeout="20">
      <providers>
        <add
          name="StateForge"
          type="StateForge.AspNet.StateForgeSessionStateProvider, StateForge.AspNet"
          rootPath="D:\StateForge"
          enableCompression="true"
          enableEncryption="false"
          keepBackups="false"
          defaultTimeoutMinutes="20"
          staleLockMinutes="5"
          shardDepth="1" />
      </providers>
    </sessionState>
  </system.web>
</configuration>
```

## IIS Application Pool Recommendations

| Setting | Recommendation |
|---|---|
| .NET CLR Version | v4.0 |
| Managed Pipeline Mode | Integrated |
| Load User Profile | True when using DPAPI |
| Start Mode | AlwaysRunning for production apps |
| Identity | Dedicated service account preferred |

## Folder Permissions

The application pool identity needs modify rights to:

```text
D:\StateForge
```

PowerShell example:

```powershell
New-Item -Path D:\StateForge -ItemType Directory -Force

icacls D:\StateForge /grant "IIS AppPool\YourAppPool:(OI)(CI)M"
```

## Validate Without IIS

```powershell
.\scripts\Invoke-StateForgeAspNetHarness.ps1 -RootPath ..\StateForgeAspNetHarness -Keep
```
