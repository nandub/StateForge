using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using StateForge.AspNetCore;
using StateForge.CloudNative;
using StateForge.Telemetry;
using StateForge.Prometheus;
using StateForge.Performance;
using StateForge.Telemetry.AspNetCore;

string root = ReadOption(args, "--root");

if (string.IsNullOrWhiteSpace(root))
{
    root = Path.Combine(Path.GetTempPath(), "StateForgeKestrelHarness", Guid.NewGuid().ToString("N"));
}

root = Path.GetFullPath(root);
Directory.CreateDirectory(root);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddStateForgeTelemetry();

builder.Services.AddStateForgeDistributedCache(options =>
{
    options.RootPath = root;
    options.EnableCompression = true;
    options.EnableEncryption = false;
    options.ShardDepth = 1;
    options.KeepBackups = false;
    StateForgeEnvironmentOptions.Apply(options);
});

WebApplication app = builder.Build();

app.MapStateForgeCloudNativeHealth();
app.MapStateForgeTelemetry();

app.MapGet("/", () => Results.Text("StateForge Kestrel Harness"));

app.MapGet("/health", async (IDistributedCache cache) =>
{
    string key = "__kestrel_health";
    byte[] payload = Encoding.UTF8.GetBytes("ok");

    StateForgeMetrics.RecordWrite();

    await cache.SetAsync(key, payload, new DistributedCacheEntryOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(5)
    });

    byte[] read = await cache.GetAsync(key);
    StateForgeMetrics.RecordRead();

    if (read == null || Encoding.UTF8.GetString(read) != "ok")
    {
        return Results.Problem("Cache health failed.");
    }

    return Results.Ok(new
    {
        healthy = true,
        rootPath = root
    });
});

app.MapPost("/session/{id}/{value}", async (string id, string value, IDistributedCache cache) =>
{
    StateForgeMetrics.RecordWrite();

    await cache.SetAsync("session:" + id, Encoding.UTF8.GetBytes(value), new DistributedCacheEntryOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(20)
    });

    return Results.Ok(new { id = id, value = value });
});

app.MapGet("/session/{id}", async (string id, IDistributedCache cache) =>
{
    byte[] value = await cache.GetAsync("session:" + id);
    StateForgeMetrics.RecordRead();

    if (value == null)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        id = id,
        value = Encoding.UTF8.GetString(value)
    });
});

app.MapDelete("/session/{id}", async (string id, IDistributedCache cache) =>
{
    await cache.RemoveAsync("session:" + id);
    StateForgeMetrics.RecordDelete();
    return Results.Ok(new { removed = id });
});

Console.WriteLine("StateForge Kestrel Harness");
Console.WriteLine("--------------------------");
Console.WriteLine("RootPath: " + root);
Console.WriteLine("URLs:");
Console.WriteLine("  GET    /health");
Console.WriteLine("  POST   /session/{id}/{value}");
Console.WriteLine("  GET    /session/{id}");
Console.WriteLine("  DELETE /session/{id}");


string stateForgePrometheusRootPath = Environment.GetEnvironmentVariable("STATEFORGE_ROOT");

if (string.IsNullOrWhiteSpace(stateForgePrometheusRootPath))
{
    stateForgePrometheusRootPath = Path.Combine(AppContext.BaseDirectory, "stateforge");
}

app.MapGet("/stateforge/prometheus", () =>
{
    string text = StateForgePrometheusCollector.CollectText(stateForgePrometheusRootPath);
    return Results.Text(text, "text/plain; version=0.0.4; charset=utf-8");
});


string stateForgeSnapshotPath = Environment.GetEnvironmentVariable("STATEFORGE_SNAPSHOT_PATH");

if (string.IsNullOrWhiteSpace(stateForgeSnapshotPath))
{
    stateForgeSnapshotPath = Path.Combine(AppContext.BaseDirectory, "stateforge-store-snapshot.json");
}

app.MapGet("/stateforge/prometheus-snapshot", () =>
{
    if (!File.Exists(stateForgeSnapshotPath))
    {
        return Results.NotFound(new { error = "Snapshot file was not found.", snapshotPath = stateForgeSnapshotPath });
    }

    string text = StateForgeSnapshotPrometheusCollector.CollectTextFromSnapshotFile(stateForgeSnapshotPath);
    return Results.Text(text, "text/plain; version=0.0.4; charset=utf-8");
});

app.MapPost("/stateforge/snapshot", () =>
{
    StateForgeStoreSnapshot snapshot = StateForgeStoreSnapshotCache.CaptureAndWrite(stateForgePrometheusRootPath, stateForgeSnapshotPath);
    return Results.Ok(snapshot);
});

app.Run();

static string ReadOption(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}
