using System;
using System.Reflection;
using StateForge.FileStore;
using StateForge.Telemetry;

namespace StateForge.Prometheus
{
    public static class StateForgePrometheusCollector
    {
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
