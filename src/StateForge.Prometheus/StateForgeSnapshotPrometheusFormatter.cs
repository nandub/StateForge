using System.Globalization;
using System.Text;
using StateForge.Performance;

namespace StateForge.Prometheus
{
    public static class StateForgeSnapshotPrometheusFormatter
    {
        public static string Format(StateForgeStoreSnapshot snapshot)
        {
            if (snapshot == null)
            {
                snapshot = new StateForgeStoreSnapshot();
            }

            StringBuilder builder = new StringBuilder();

            Gauge(builder, "stateforge_sessions_active", "Snapshot StateForge session count.", snapshot.TotalSessions);
            Gauge(builder, "stateforge_sessions_expired", "Snapshot expired StateForge session count.", snapshot.ExpiredSessions);
            Gauge(builder, "stateforge_sessions_locked", "Snapshot locked StateForge session count.", snapshot.LockedSessions);
            Gauge(builder, "stateforge_sessions_compressed", "Snapshot compressed StateForge session count.", snapshot.CompressedSessions);
            Gauge(builder, "stateforge_sessions_encrypted", "Snapshot encrypted StateForge session count.", snapshot.EncryptedSessions);
            Gauge(builder, "stateforge_sessions_aes_encrypted", "Snapshot AES encrypted StateForge session count.", snapshot.AesEncryptedSessions);
            Gauge(builder, "stateforge_payload_bytes_total", "Snapshot total StateForge payload bytes.", snapshot.TotalPayloadBytes);
            Gauge(builder, "stateforge_average_payload_bytes", "Snapshot average StateForge payload bytes.", snapshot.AveragePayloadBytes);
            Gauge(builder, "stateforge_snapshot_capture_elapsed_ms", "Elapsed milliseconds spent capturing the snapshot.", snapshot.CaptureElapsedMs);
            Gauge(builder, "stateforge_snapshot_age_seconds", "Age of the snapshot in seconds.", StateForgeSnapshotAge.GetAgeSeconds(snapshot));

            return builder.ToString();
        }

        private static void Gauge(StringBuilder builder, string name, string help, long value)
        {
            builder.Append("# HELP ").Append(name).Append(" ").Append(help).AppendLine();
            builder.Append("# TYPE ").Append(name).AppendLine(" gauge");
            builder.Append(name).Append(" ").Append(value.ToString(CultureInfo.InvariantCulture)).AppendLine();
            builder.AppendLine();
        }

        private static void Gauge(StringBuilder builder, string name, string help, double value)
        {
            builder.Append("# HELP ").Append(name).Append(" ").Append(help).AppendLine();
            builder.Append("# TYPE ").Append(name).AppendLine(" gauge");
            builder.Append(name).Append(" ").Append(value.ToString("0.###", CultureInfo.InvariantCulture)).AppendLine();
            builder.AppendLine();
        }
    }
}
