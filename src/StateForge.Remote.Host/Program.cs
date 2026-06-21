using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using StateForge.Core;
using StateForge.FileStore;
using StateForge.Remote.Host;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string endpoint = ReadRequired("STATEFORGE_REMOTE_LISTEN", "tcp:0.0.0.0:7443");
string certificatePath = ReadRequired("STATEFORGE_REMOTE_TLS_CERT_PATH", null);
string certificatePassword = ReadRequired("STATEFORGE_REMOTE_TLS_CERT_PASSWORD", null);
string rootPath = ReadRequired("STATEFORGE_ROOT_PATH", "/data/stateforge");
string aesKey = ReadRequired("STATEFORGE_AES_KEY_BASE64", null);
string bearerToken = Environment.GetEnvironmentVariable("STATEFORGE_REMOTE_BEARER_TOKEN") ?? string.Empty;

Uri listenAddress = ParseListenEndpoint(endpoint);
X509Certificate2 certificate = new X509Certificate2(certificatePath, certificatePassword);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Parse(listenAddress.Host), listenAddress.Port, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
        listenOptions.UseHttps(certificate);
    });
});

builder.Services.AddGrpc(options =>
{
    options.EnableDetailedErrors = false;
    options.MaxReceiveMessageSize = 16 * 1024 * 1024;
    options.MaxSendMessageSize = 16 * 1024 * 1024;
});

builder.Services.AddSingleton<IStateForgeStore>(_ =>
    new StateForgeFileStore(new StateForgeFileStoreOptions
    {
        RootPath = rootPath,
        EnableCompression = true,
        EnableEncryption = true,
        ProtectionMode = StateForgeProtectionMode.Aes,
        AesKeyBase64 = aesKey,
        KeepBackups = false,
        ShardDepth = 1
    }));

WebApplication app = builder.Build();

if (!string.IsNullOrWhiteSpace(bearerToken))
{
    app.Use(async (context, next) =>
    {
        if (HttpMethods.IsGet(context.Request.Method) &&
            string.Equals(context.Request.Path, "/livez", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        string expected = "Bearer " + bearerToken;
        string actual = context.Request.Headers["Authorization"].ToString();
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await next();
    });
}

app.MapGrpcService<StateForgeStoreGrpcService>();
app.MapGet("/livez", () => Results.Ok(new { healthy = true }));

app.Run();

static string ReadRequired(string name, string fallback)
{
    string value = Environment.GetEnvironmentVariable(name);

    if (!string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    if (fallback != null)
    {
        return fallback;
    }

    throw new InvalidOperationException(name + " is required.");
}

static Uri ParseListenEndpoint(string endpoint)
{
    if (string.IsNullOrWhiteSpace(endpoint) ||
        !endpoint.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException("STATEFORGE_REMOTE_LISTEN must use tcp:IP:PORT.");
    }

    string authority = endpoint.Substring("tcp:".Length);
    int separator = authority.LastIndexOf(':');

    if (separator <= 0 || separator == authority.Length - 1)
    {
        throw new InvalidOperationException("STATEFORGE_REMOTE_LISTEN must use tcp:IP:PORT.");
    }

    int port;
    if (!int.TryParse(authority.Substring(separator + 1), out port))
    {
        throw new InvalidOperationException("STATEFORGE_REMOTE_LISTEN port must be numeric.");
    }

    return new UriBuilder(Uri.UriSchemeHttps, authority.Substring(0, separator), port).Uri;
}
