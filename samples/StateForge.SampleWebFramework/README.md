# ASP.NET Framework Web Forms Sample

This sample demonstrates `StateForgeSessionStateProvider` as a custom ASP.NET Framework session-state
provider. `Default.aspx` stores an integer counter in `System.Web.SessionState`.

This integration remains relevant for ASP.NET Web Forms and MVC 5 applications running on Windows and
IIS. It is not an ASP.NET Core sample.

## Prerequisites

- Windows
- .NET Framework 4.8.1 Developer Pack
- Visual Studio with the ASP.NET and web development workload
- IIS Express or IIS with ASP.NET 4.x enabled
- Write permission for the application-pool identity

## Create and Run the Web Application

1. Create an **ASP.NET Web Application (.NET Framework)** targeting .NET Framework 4.8.1.
2. Choose the Empty or Web Forms template.
3. Copy `Default.aspx` and `Web.config` from this folder into the web project.
4. Reference `StateForge.AspNet.dll` and its StateForge dependencies, or reference
   `src\StateForge.AspNet\StateForge.AspNet.csproj` while developing in this repository.
5. Build and run with IIS Express.
6. Refresh `Default.aspx`; the counter should increase.
7. Restart the application and refresh again to verify disk persistence.

When `rootPath` is omitted, the provider stores data under:

```text
<application-root>\App_Data\StateForge
```

The provider creates the directory, but the application-pool identity must be able to write to it.

## Provider Configuration

The supplied `Web.config` registers:

```xml
<sessionState mode="Custom" customProvider="StateForge" timeout="20">
  <providers>
    <add name="StateForge"
         type="StateForge.AspNet.StateForgeSessionStateProvider, StateForge.AspNet"
         staleLockMinutes="5"
         defaultTimeoutMinutes="20"
         shardDepth="1"
         maxPayloadBytes="104857600"
         mutexTimeoutMilliseconds="30000"
         enableCompression="true"
         enableEncryption="false"
         keepBackups="false"
         protectionMode="none" />
  </providers>
</sessionState>
```

Set `rootPath` explicitly when storage should live outside the application:

```xml
rootPath="D:\StateForge\SessionStore"
```

Use a path outside the web root in production.

## Enable AES

The provider accepts `protectionMode="aes"` and `aesKeyBase64`. A literal key in `Web.config` is shown
only for configuration clarity and should not be committed:

```xml
enableEncryption="true"
protectionMode="aes"
aesKeyBase64="BASE64_ENCODED_AES_KEY"
```

Protect configuration sections with IIS/.NET protected configuration or inject the value during
deployment. Every application instance sharing the store must use the same key.

DPAPI is available with `protectionMode="dpapi"`, but machine-local DPAPI is generally unsuitable for a
shared multi-node store because another machine may not be able to decrypt the payload.

## IIS Permissions

Grant Modify permission only to the application-pool identity:

```powershell
.\scripts\Install-StateForgeStore.ps1 `
  -RootPath D:\StateForge\SessionStore `
  -Identity "IIS AppPool\MyApplicationPool"
```

Do not grant broad write access such as `Everyone` or `Users`.

## Scale-Out and Operations

- Use a shared root reachable by all IIS nodes.
- Keep provider settings identical on every node.
- Share the ASP.NET `<machineKey>` separately when the application requires cross-node authentication
  or anti-forgery compatibility.
- Schedule cleanup and monitor health, storage capacity, quarantine, and replica status.
- Drain older writers before changing shard depth or encryption behavior.

StateForge stores session payloads; it does not synchronize application binaries, `machineKey`, or other
ASP.NET configuration.
