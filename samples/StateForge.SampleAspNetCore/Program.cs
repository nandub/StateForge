using StateForge.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStateForgeDistributedCache(options =>
{
    options.RootPath = @"D:\StateForge";
    options.StaleLockMinutes = 5;
    options.DefaultExpirationMinutes = 20;
    options.EnableCompression = true;
    options.EnableEncryption = true;
});

builder.Services.AddSession();

var app = builder.Build();

app.UseSession();

app.MapGet("/", context =>
{
    int value = context.Session.GetInt32("Counter") ?? 0;
    value++;
    context.Session.SetInt32("Counter", value);
    return context.Response.WriteAsync("StateForge Counter: " + value);
});

app.Run();
