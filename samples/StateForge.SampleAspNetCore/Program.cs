using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StateForge.Core;
using StateForge.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var rootPath = Environment.GetEnvironmentVariable("STATEFORGE_ROOT_PATH");
var aesKey = Environment.GetEnvironmentVariable("STATEFORGE_AES_KEY_BASE64");
var dataProtectionPath = Environment.GetEnvironmentVariable("STATEFORGE_DATA_PROTECTION_PATH");

if (string.IsNullOrWhiteSpace(rootPath))
{
    rootPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "StateForge");
}

if (string.IsNullOrWhiteSpace(dataProtectionPath))
{
    dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
}

dataProtectionPath = Path.GetFullPath(dataProtectionPath);
Directory.CreateDirectory(dataProtectionPath);

builder.Services
    .AddDataProtection()
    .SetApplicationName("StateForge.SampleAspNetCore")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

builder.Services.AddStateForgeDistributedCache(options =>
{
    options.RootPath = Path.GetFullPath(rootPath);
    options.StaleLockMinutes = 5;
    options.DefaultExpirationMinutes = 20;
    options.ShardDepth = 1;
    options.EnableCompression = true;
    options.KeepBackups = false;

    if (!string.IsNullOrWhiteSpace(aesKey))
    {
        options.EnableEncryption = true;
        options.ProtectionMode = StateForgeProtectionMode.Aes;
        options.AesKeyBase64 = aesKey;
    }
});

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(20);
});

var app = builder.Build();

app.UseSession();

app.MapGet("/", async context =>
{
    int value = context.Session.GetInt32("Counter") ?? 0;
    value++;
    context.Session.SetInt32("Counter", value);
    await context.Response.WriteAsync(
        "StateForge session counter: " + value + Environment.NewLine +
        "Store root: " + Path.GetFullPath(rootPath));
});

app.Run();
