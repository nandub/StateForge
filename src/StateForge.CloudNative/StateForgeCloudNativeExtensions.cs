using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using StateForge.AspNetCore;
using StateForge.FileStore;

namespace StateForge.CloudNative
{
    /// <summary>Registers container-oriented StateForge defaults and Kubernetes health endpoints.</summary>
    public static class StateForgeCloudNativeExtensions
    {
        /// <summary>Registers StateForge as the distributed cache using environment-driven container defaults.</summary>
        /// <param name="services">The application service collection.</param>
        /// <returns><paramref name="services"/> for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
        /// <example>
        /// Configure a minimal cloud-native application:
        /// <code language="csharp">
        /// builder.Services.AddStateForgeCloudNativeCache();
        /// builder.Services.AddSession();
        /// WebApplication app = builder.Build();
        /// app.MapStateForgeCloudNativeHealth();
        /// </code>
        /// </example>
        public static IServiceCollection AddStateForgeCloudNativeCache(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException("services");
            }

            services.AddStateForgeDistributedCache(delegate(StateForgeDistributedCacheOptions options)
            {
                ApplyDefaultIfPresent(options, "RootPath", "/data/stateforge");
                ApplyDefaultIfPresent(options, "EnableCompression", true);
                ApplyDefaultIfPresent(options, "EnableEncryption", false);
                ApplyDefaultIfPresent(options, "KeepBackups", false);
                ApplyDefaultIfPresent(options, "ShardDepth", 1);

                StateForgeEnvironmentOptions.Apply(options);
            });

            return services;
        }

        /// <summary>Maps <c>/livez</c>, <c>/readyz</c>, and <c>/healthz</c> endpoints.</summary>
        /// <param name="app">The application endpoint builder.</param>
        /// <returns><paramref name="app"/> for chaining.</returns>
        public static WebApplication MapStateForgeCloudNativeHealth(this WebApplication app)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            app.MapGet("/livez", delegate()
            {
                return Results.Ok(new { alive = true });
            });

            app.MapGet("/readyz", delegate()
            {
                StateForgeFileStore store = StateForgeHealthStoreFactory.CreateFromEnvironment();
                StateForge.Core.StateForgeHealthResult result = store.CheckHealth();

                if (!result.Healthy)
                {
                    return Results.Json(new
                    {
                        ready = false,
                        canRead = result.CanRead,
                        canWrite = result.CanWrite,
                        canLock = result.CanLock,
                        canEnumerate = result.CanEnumerate,
                        canCleanup = result.CanCleanup,
                        errors = result.Errors
                    }, statusCode: 503);
                }

                return Results.Ok(new
                {
                    ready = true,
                    canRead = result.CanRead,
                    canWrite = result.CanWrite,
                    canLock = result.CanLock,
                    canEnumerate = result.CanEnumerate,
                    canCleanup = result.CanCleanup
                });
            });

            app.MapGet("/healthz", delegate()
            {
                StateForgeFileStore store = StateForgeHealthStoreFactory.CreateFromEnvironment();
                StateForge.Core.StateForgeHealthResult health = store.CheckHealth();
                StateForge.Core.StateForgeStoreDiagnostics diagnostics = store.GetDiagnostics();
                StateForge.Core.StateForgeStoreStats stats = store.GetStats();

                return Results.Json(new
                {
                    healthy = health.Healthy,
                    health = new
                    {
                        canRead = health.CanRead,
                        canWrite = health.CanWrite,
                        canLock = health.CanLock,
                        canEnumerate = health.CanEnumerate,
                        canCleanup = health.CanCleanup,
                        errors = health.Errors
                    },
                    diagnostics = new
                    {
                        rootPath = diagnostics.RootPath,
                        sessions = diagnostics.SessionFileCount,
                        temp = diagnostics.TempFileCount,
                        backups = diagnostics.BackupFileCount,
                        quarantine = diagnostics.QuarantineFileCount
                    },
                    stats = new
                    {
                        totalSessions = stats.TotalSessions,
                        expiredSessions = stats.ExpiredSessions,
                        lockedSessions = stats.LockedSessions,
                        compressedSessions = stats.CompressedSessions,
                        encryptedSessions = stats.EncryptedSessions,
                        aesEncryptedSessions = stats.AesEncryptedSessions,
                        totalPayloadBytes = stats.TotalPayloadBytes,
                        averagePayloadBytes = stats.AveragePayloadBytes
                    }
                }, statusCode: health.Healthy ? 200 : 503);
            });

            return app;
        }

        private static void ApplyDefaultIfPresent(object options, string propertyName, object value)
        {
            System.Reflection.PropertyInfo property = options.GetType().GetProperty(propertyName);

            if (property == null || !property.CanWrite)
            {
                return;
            }

            property.SetValue(options, value, null);
        }
    }
}
