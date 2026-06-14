using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using StateForge.CloudNative;
using StateForge.Telemetry;
using StateForge.Telemetry.AspNetCore;

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("STATEFORGE_ROOT_PATH")))
{
    Environment.SetEnvironmentVariable(
        "STATEFORGE_ROOT_PATH",
        Path.Combine(AppContext.BaseDirectory, "App_Data", "StateForge"));
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddStateForgeCloudNativeCache();
builder.Services.AddStateForgeTelemetry();

var app = builder.Build();
app.MapStateForgeCloudNativeHealth();
app.MapStateForgeTelemetry();

app.MapPut("/cache/{key}", async (string key, CacheValue request, IDistributedCache cache) =>
{
    await cache.SetAsync(
        "sample:" + key,
        Encoding.UTF8.GetBytes(request.Value ?? string.Empty),
        new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(20)
        });

    StateForgeMetrics.RecordWrite();
    return Results.NoContent();
});

app.MapGet("/cache/{key}", async (string key, IDistributedCache cache) =>
{
    byte[] value = await cache.GetAsync("sample:" + key);
    StateForgeMetrics.RecordRead();

    return value == null
        ? Results.NotFound()
        : Results.Ok(new CacheValue { Value = Encoding.UTF8.GetString(value) });
});

app.MapDelete("/cache/{key}", async (string key, IDistributedCache cache) =>
{
    await cache.RemoveAsync("sample:" + key);
    StateForgeMetrics.RecordDelete();
    return Results.NoContent();
});

app.Run();

internal sealed class CacheValue
{
    public string Value { get; set; }
}
