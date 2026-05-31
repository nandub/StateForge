# ASP.NET Core IDistributedCache Provider

This guide shows how to configure StateForge in an ASP.NET Core application, use it through `IDistributedCache`, and validate it with the Kestrel harness.

## Step 1: Reference StateForge

During local development, reference these projects:

```text
src\StateForge.Core
src\StateForge.FileStore
src\StateForge.AspNetCore
```

For deployment, include these assemblies with your ASP.NET Core application:

```text
StateForge.Core.dll
StateForge.FileStore.dll
StateForge.AspNetCore.dll
```

## Step 2: Create the Storage Folder

```powershell
New-Item -Path D:\StateForge -ItemType Directory -Force
```

Grant the application identity modify rights:

```powershell
icacls D:\StateForge /grant "DOMAIN\svc-web:(OI)(CI)M"
```

For local development, you can use a relative path such as:

```text
..\StateForgeKestrel
```

## Step 3: Register StateForge in Program.cs

Add the namespace:

```csharp
using StateForge.AspNetCore;
```

Register the provider:

```csharp
builder.Services.AddStateForgeDistributedCache(options =>
{
    options.RootPath = @"D:\StateForge";
    options.EnableCompression = true;
    options.EnableEncryption = false;
    options.ShardDepth = 1;
    options.KeepBackups = false;
});
```

## Step 4: Use IDistributedCache from a Service

Create `ExampleService.cs`:

```csharp
using System.Text;
using Microsoft.Extensions.Caching.Distributed;

public sealed class ExampleService
{
    private readonly IDistributedCache _cache;

    public ExampleService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task SaveValueAsync(string key, string value)
    {
        byte[] payload = Encoding.UTF8.GetBytes(value);

        DistributedCacheEntryOptions options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(20)
        };

        await _cache.SetAsync(key, payload, options);
    }

    public async Task<string> GetValueAsync(string key)
    {
        byte[] payload = await _cache.GetAsync(key);

        if (payload == null)
        {
            return null;
        }

        return Encoding.UTF8.GetString(payload);
    }

    public async Task RemoveValueAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    public async Task RefreshValueAsync(string key)
    {
        await _cache.RefreshAsync(key);
    }
}
```

## Step 5: Register ExampleService

```csharp
builder.Services.AddScoped<ExampleService>();
```

Complete minimal startup example:

```csharp
using StateForge.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddStateForgeDistributedCache(options =>
{
    options.RootPath = @"D:\StateForge";
    options.EnableCompression = true;
    options.EnableEncryption = false;
    options.ShardDepth = 1;
    options.KeepBackups = false;
});

builder.Services.AddScoped<ExampleService>();

WebApplication app = builder.Build();

app.MapGet("/", () => "StateForge ASP.NET Core sample");

app.Run();
```

## Step 6: Use ExampleService from Minimal API Endpoints

```csharp
app.MapPost("/cache/{key}/{value}", async (
    string key,
    string value,
    ExampleService service) =>
{
    await service.SaveValueAsync(key, value);
    return Results.Ok(new { key, value });
});

app.MapGet("/cache/{key}", async (
    string key,
    ExampleService service) =>
{
    string value = await service.GetValueAsync(key);

    if (value == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new { key, value });
});

app.MapDelete("/cache/{key}", async (
    string key,
    ExampleService service) =>
{
    await service.RemoveValueAsync(key);
    return Results.Ok(new { removed = key });
});
```

## Step 7: Use ExampleService from a Controller

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/cache")]
public sealed class CacheController : ControllerBase
{
    private readonly ExampleService _service;

    public CacheController(ExampleService service)
    {
        _service = service;
    }

    [HttpPost("{key}/{value}")]
    public async Task<IActionResult> Set(string key, string value)
    {
        await _service.SaveValueAsync(key, value);
        return Ok(new { key, value });
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        string value = await _service.GetValueAsync(key);

        if (value == null)
        {
            return NotFound();
        }

        return Ok(new { key, value });
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> Delete(string key)
    {
        await _service.RemoveValueAsync(key);
        return Ok(new { removed = key });
    }
}
```

## Step 8: Use StateForge With ASP.NET Core Session

ASP.NET Core Session uses `IDistributedCache`, so StateForge can back ASP.NET Core session data.

```csharp
using StateForge.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddStateForgeDistributedCache(options =>
{
    options.RootPath = @"D:\StateForge";
    options.EnableCompression = true;
    options.EnableEncryption = false;
    options.ShardDepth = 1;
    options.KeepBackups = false;
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

WebApplication app = builder.Build();

app.UseSession();

app.MapGet("/session/set/{value}", (string value, HttpContext context) =>
{
    context.Session.SetString("demo", value);
    return Results.Ok(new { value });
});

app.MapGet("/session/get", (HttpContext context) =>
{
    string value = context.Session.GetString("demo");

    if (value == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new { value });
});

app.Run();
```

## Step 9: AES Configuration for ASP.NET Core

Generate a key:

```powershell
$key = dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj -- generate-key
```

Configure AES:

```csharp
builder.Services.AddStateForgeDistributedCache(options =>
{
    options.RootPath = @"\\FileServer\StateForge";
    options.EnableCompression = true;
    options.EnableEncryption = true;
    options.ProtectionMode = StateForgeProtectionMode.Aes;
    options.AesKeyBase64 = "<base64-key>";
    options.ShardDepth = 1;
    options.KeepBackups = false;
});
```

Use the same AES key on every farm node.

## Step 10: Validate With the Kestrel Harness

Terminal 1:

```powershell
.\scripts\Start-StateForgeKestrelHarness.ps1 `
    -RootPath ..\StateForgeKestrel `
    -Url http://localhost:5075
```

Terminal 2:

```powershell
.\scripts\Test-StateForgeKestrelHarness.ps1 `
    -Url http://localhost:5075
```

Expected:

```text
PASS: Kestrel health
PASS: Kestrel set
PASS: Kestrel get
PASS: Kestrel delete
```

## Step 11: Inspect the Store

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- diag `
    --root D:\StateForge
```

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- list `
    --root D:\StateForge `
    --format json
```

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- stats `
    --root D:\StateForge `
    --format json
```

For AES-protected records:

```powershell
dotnet run --project .\src\StateForge.Tools\StateForge.Tools.csproj `
    -- list `
    --root D:\StateForge `
    --format json `
    --protection aes `
    --aes-key "<base64-key>"
```

## Troubleshooting

If the provider does not appear to store values:

1. Confirm `RootPath` exists.
2. Confirm the application identity can modify `RootPath`.
3. Run the Kestrel harness.
4. Run `StateForge.Tools health`.
5. Check whether records are AES-protected and require `--aes-key` for CLI inspection.
