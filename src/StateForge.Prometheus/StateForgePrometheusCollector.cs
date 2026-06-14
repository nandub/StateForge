using System;
using System.Reflection;
using StateForge.FileStore;
using StateForge.Telemetry;

namespace StateForge.Prometheus
{
    /// <summary>Collects StateForge runtime and store statistics in Prometheus-compatible form.</summary>
    public static class StateForgePrometheusCollector
    {
        /// <summary>Captures process-local telemetry counters and optional file-store statistics.</summary>
        /// <param name="rootPath">The StateForge root path, or a blank value to omit store statistics.</param>
        /// <returns>A snapshot suitable for Prometheus formatting.</returns>
        public static StateForgePrometheusSnapshot Collect(string rootPath)
        {
            StateForgePrometheusSnapshot snapshot = new StateForgePrometheusSnapshot();
            object metricSnapshot = CaptureMetricsSnapshot();

            snapshot.Reads = ReadLong(metricSnapshot, "Reads");
            snapshot.Writes = ReadLong(metricSnapshot, "Writes");
            snapshot.Deletes = ReadLong(metricSnapshot, "Deletes");
            snapshot.LocksAcquired = ReadLong(metricSnapshot, "LocksAcquired");
            snapshot.LockContentions = ReadLong(metricSnapshot, "LockContentions");
            snapshot.Cleanups = ReadLong(metricSnapshot, "Cleanups");
            snapshot.Quarantines = ReadLong(metricSnapshot, "Quarantines");
            snapshot.Corruptions = ReadLong(metricSnapshot, "Corruptions");

            if (!string.IsNullOrWhiteSpace(rootPath))
            {
                StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
                options.RootPath = rootPath;

                StateForgeFileStore store = new StateForgeFileStore(options);
                object stats = store.GetStats();

                snapshot.SessionsActive = ReadInt(stats, "TotalSessions");
                snapshot.SessionsExpired = ReadInt(stats, "ExpiredSessions");
                snapshot.SessionsLocked = ReadInt(stats, "LockedSessions");
                snapshot.SessionsCompressed = ReadInt(stats, "CompressedSessions");
                snapshot.SessionsEncrypted = ReadInt(stats, "EncryptedSessions");
                snapshot.SessionsAesEncrypted = ReadInt(stats, "AesEncryptedSessions");
                snapshot.TotalPayloadBytes = ReadLong(stats, "TotalPayloadBytes");
            }

            return snapshot;
        }

        /// <summary>Collects and formats StateForge metrics in the Prometheus text exposition format.</summary>
        /// <param name="rootPath">The StateForge store root, or a blank value to omit store statistics.</param>
        /// <returns>Prometheus text containing process metrics and, when requested, store statistics.</returns>
        /// <example>
        /// Return the metrics from an ASP.NET Core endpoint:
        /// <code language="csharp">
        /// app.MapGet("/metrics", (IConfiguration configuration) =>
        /// {
        ///     string rootPath = configuration["StateForge:RootPath"];
        ///     string metrics = StateForgePrometheusCollector.CollectText(rootPath);
        ///     return Results.Text(metrics, "text/plain; version=0.0.4");
        /// });
        /// </code>
        /// </example>
        public static string CollectText(string rootPath)
        {
            return StateForgePrometheusFormatter.Format(Collect(rootPath));
        }

        private static object CaptureMetricsSnapshot()
        {
            Type metricsType = typeof(StateForgeMetrics);
            string[] methodNames = new string[] { "Snapshot", "GetSnapshot", "CaptureSnapshot", "Capture", "GetMetrics", "GetMetricSnapshot" };

            for (int i = 0; i < methodNames.Length; i++)
            {
                MethodInfo method = metricsType.GetMethod(
                    methodNames[i],
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    Type.EmptyTypes,
                    null);

                if (method != null)
                {
                    return method.Invoke(null, null);
                }
            }

            return null;
        }

        private static long ReadLong(object instance, string propertyName)
        {
            if (instance == null)
            {
                return 0;
            }

            PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (property == null)
            {
                return 0;
            }

            object value = property.GetValue(instance, null);

            if (value == null)
            {
                return 0;
            }

            return Convert.ToInt64(value);
        }

        private static int ReadInt(object instance, string propertyName)
        {
            return Convert.ToInt32(ReadLong(instance, propertyName));
        }
    }
}
