# StateForge

StateForge is a file-based session state and distributed cache platform for .NET Framework and ASP.NET Core.

## Packages

- StateForge.Core
- StateForge.FileStore
- StateForge.AspNet
- StateForge.AspNetCore
- StateForge.Security
- StateForge.Telemetry
- StateForge.CloudNative
- StateForge.Format
- StateForge.Prometheus

## ASP.NET Core Example

```csharp
builder.Services.AddStateForgeDistributedCache(options =>
{
    options.RootPath = @"D:\StateForge";
});
```

## ASP.NET Framework Example

```xml
<sessionState mode="Custom" customProvider="StateForge">
  <providers>
    <add name="StateForge"
         type="StateForge.AspNet.StateForgeSessionStateStoreProvider, StateForge.AspNet"
         rootPath="D:\StateForge" />
  </providers>
</sessionState>
```

Version: 0.17.2
