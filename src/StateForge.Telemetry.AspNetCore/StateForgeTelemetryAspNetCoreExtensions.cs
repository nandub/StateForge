using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using StateForge.Telemetry;

namespace StateForge.Telemetry.AspNetCore
{
    public static class StateForgeTelemetryAspNetCoreExtensions
    {
        public static IServiceCollection AddStateForgeTelemetry(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException("services");
            }

            return services;
        }

        public static WebApplication MapStateForgeTelemetry(this WebApplication app)
        {
            if (app == null)
            {
                throw new ArgumentNullException("app");
            }

            app.MapGet("/stateforge/metrics", delegate()
            {
                StateForgeMetricSnapshot snapshot = StateForgeMetrics.Snapshot();

                return Results.Ok(new
                {
                    capturedUtc = snapshot.CapturedUtc,
                    reads = snapshot.Reads,
                    writes = snapshot.Writes,
                    deletes = snapshot.Deletes,
                    locksAcquired = snapshot.LocksAcquired,
                    lockContentions = snapshot.LockContentions,
                    cleanups = snapshot.Cleanups,
                    quarantines = snapshot.Quarantines,
                    corruptions = snapshot.Corruptions
                });
            });

            app.MapPost("/stateforge/metrics/reset", delegate()
            {
                StateForgeMetrics.Reset();

                return Results.Ok(new
                {
                    reset = true
                });
            });

            return app;
        }
    }
}
