using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using StateForge.Core;
using StateForge.FileStore;
using StateForge.Remote.Host;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

string endpoint = ReadRequired("STATEFORGE_REMOTE_LISTEN", "tcp:0.0.0.0:7443");
string certificatePath = Environment.GetEnvironmentVariable("STATEFORGE_REMOTE_TLS_CERT_PATH") ?? string.Empty;
string certificatePassword = Environment.GetEnvironmentVariable("STATEFORGE_REMOTE_TLS_CERT_PASSWORD") ?? string.Empty;
string certificatePemPath = Environment.GetEnvironmentVariable("STATEFORGE_REMOTE_TLS_CERT_PEM_PATH") ?? string.Empty;
string certificateKeyPemPath = Environment.GetEnvironmentVariable("STATEFORGE_REMOTE_TLS_KEY_PEM_PATH") ?? string.Empty;
string rootPath = ReadRequired("STATEFORGE_ROOT_PATH", "/data/stateforge");
string aesKey = ReadRequired("STATEFORGE_AES_KEY_BASE64", null);
string bearerToken = Environment.GetEnvironmentVariable("STATEFORGE_REMOTE_BEARER_TOKEN") ?? string.Empty;
string adminBearerToken = Environment.GetEnvironmentVariable("STATEFORGE_REMOTE_ADMIN_BEARER_TOKEN") ?? string.Empty;
bool allowUnauthenticated = ReadBooleanEnvironment("STATEFORGE_REMOTE_ALLOW_UNAUTHENTICATED");

if (!allowUnauthenticated && string.IsNullOrWhiteSpace(bearerToken))
{
    throw new InvalidOperationException(
        "STATEFORGE_REMOTE_BEARER_TOKEN is required. Set STATEFORGE_REMOTE_ALLOW_UNAUTHENTICATED=true only for isolated development.");
}

if (!string.IsNullOrWhiteSpace(adminBearerToken) &&
    string.Equals(bearerToken, adminBearerToken, StringComparison.Ordinal))
{
    throw new InvalidOperationException("STATEFORGE_REMOTE_ADMIN_BEARER_TOKEN must be distinct from STATEFORGE_REMOTE_BEARER_TOKEN.");
}

Uri listenAddress = ParseListenEndpoint(endpoint);
X509Certificate2 certificate = LoadTlsCertificate(
    certificatePath,
    certificatePassword,
    certificatePemPath,
    certificateKeyPemPath);

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

if (!allowUnauthenticated)
{
    app.Use(async (context, next) =>
    {
        if (HttpMethods.IsGet(context.Request.Method) &&
            string.Equals(context.Request.Path, "/livez", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        bool isAdminRequest = IsAdminRequest(context.Request.Path);
        bool authorized = IsAuthorized(context, bearerToken) ||
            (!string.IsNullOrWhiteSpace(adminBearerToken) && IsAuthorized(context, adminBearerToken));

        if (!authorized)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        if (isAdminRequest && !IsAuthorized(context, adminBearerToken))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Forbidden");
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

static bool ReadBooleanEnvironment(string name)
{
    string value = Environment.GetEnvironmentVariable(name);
    return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
}

static bool IsAdminRequest(PathString path)
{
    string value = path.Value ?? string.Empty;
    return string.Equals(value, "/stateforge.remote.v1.StateForgeStoreRpc/Enumerate", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "/stateforge.remote.v1.StateForgeStoreRpc/GetDiagnostics", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "/stateforge.remote.v1.StateForgeStoreRpc/CleanupExpired", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "/stateforge.remote.v1.StateForgeStoreRpc/ForceRemove", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "/stateforge.remote.v1.StateForgeStoreRpc/GetStats", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "/stateforge.remote.v1.StateForgeStoreRpc/ValidateConfiguration", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(value, "/stateforge.remote.v1.StateForgeStoreRpc/CheckHealth", StringComparison.OrdinalIgnoreCase);
}

static bool IsAuthorized(HttpContext context, string token)
{
    if (string.IsNullOrWhiteSpace(token))
    {
        return false;
    }

    string header = context.Request.Headers["Authorization"].ToString();
    const string bearerPrefix = "Bearer ";
    if (header.Length <= bearerPrefix.Length ||
        !header.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    byte[] actual = Encoding.UTF8.GetBytes(header.Substring(bearerPrefix.Length));
    byte[] expected = Encoding.UTF8.GetBytes(token);
    return CryptographicOperations.FixedTimeEquals(actual, expected);
}

static X509Certificate2 LoadTlsCertificate(
    string certificatePath,
    string certificatePassword,
    string certificatePemPath,
    string certificateKeyPemPath)
{
    if (!string.IsNullOrWhiteSpace(certificatePemPath) ||
        !string.IsNullOrWhiteSpace(certificateKeyPemPath))
    {
        if (string.IsNullOrWhiteSpace(certificatePemPath) ||
            string.IsNullOrWhiteSpace(certificateKeyPemPath))
        {
            throw new InvalidOperationException(
                "STATEFORGE_REMOTE_TLS_CERT_PEM_PATH and STATEFORGE_REMOTE_TLS_KEY_PEM_PATH must be set together.");
        }

        return X509Certificate2.CreateFromPemFile(certificatePemPath, certificateKeyPemPath);
    }

    if (string.IsNullOrWhiteSpace(certificatePath))
    {
        throw new InvalidOperationException(
            "STATEFORGE_REMOTE_TLS_CERT_PATH or STATEFORGE_REMOTE_TLS_CERT_PEM_PATH is required.");
    }

    if (string.IsNullOrWhiteSpace(certificatePassword))
    {
        throw new InvalidOperationException("STATEFORGE_REMOTE_TLS_CERT_PASSWORD is required for PFX certificates.");
    }

    return new X509Certificate2(certificatePath, certificatePassword);
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
