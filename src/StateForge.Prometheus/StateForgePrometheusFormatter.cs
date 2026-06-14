using System.Globalization;
using System.Text;

namespace StateForge.Prometheus
{
    /// <summary>Provides state forge prometheus formatter operations.</summary>
    public static class StateForgePrometheusFormatter
    {
        /// <summary>Formats a metric snapshot using the Prometheus text exposition format.</summary>
        public static string Format(StateForgePrometheusSnapshot snapshot)
        {
            if (snapshot == null)
            {
                snapshot = new StateForgePrometheusSnapshot();
            }

            StringBuilder builder = new StringBuilder();

            Counter(builder, "stateforge_reads_total", "Total StateForge read operations.", snapshot.Reads);
            Counter(builder, "stateforge_writes_total", "Total StateForge write operations.", snapshot.Writes);
            Counter(builder, "stateforge_deletes_total", "Total StateForge delete operations.", snapshot.Deletes);
            Counter(builder, "stateforge_locks_acquired_total", "Total StateForge locks acquired.", snapshot.LocksAcquired);
            Counter(builder, "stateforge_lock_contentions_total", "Total StateForge lock contentions.", snapshot.LockContentions);
            Counter(builder, "stateforge_cleanup_runs_total", "Total StateForge cleanup runs.", snapshot.Cleanups);
            Counter(builder, "stateforge_quarantine_total", "Total StateForge quarantine events.", snapshot.Quarantines);
            Counter(builder, "stateforge_corruption_total", "Total StateForge corruption events.", snapshot.Corruptions);

            Gauge(builder, "stateforge_sessions_active", "Current StateForge session count.", snapshot.SessionsActive);
            Gauge(builder, "stateforge_sessions_expired", "Current expired StateForge session count.", snapshot.SessionsExpired);
            Gauge(builder, "stateforge_sessions_locked", "Current locked StateForge session count.", snapshot.SessionsLocked);
            Gauge(builder, "stateforge_sessions_compressed", "Current compressed StateForge session count.", snapshot.SessionsCompressed);
            Gauge(builder, "stateforge_sessions_encrypted", "Current encrypted StateForge session count.", snapshot.SessionsEncrypted);
            Gauge(builder, "stateforge_sessions_aes_encrypted", "Current AES-encrypted StateForge session count.", snapshot.SessionsAesEncrypted);
            Gauge(builder, "stateforge_payload_bytes_total", "Current total StateForge payload bytes.", snapshot.TotalPayloadBytes);

            return builder.ToString();
        }

        private static void Counter(StringBuilder builder, string name, string help, long value)
        {
            builder.Append("# HELP ").Append(name).Append(" ").Append(help).AppendLine();
            builder.Append("# TYPE ").Append(name).AppendLine(" counter");
            builder.Append(name).Append(" ").Append(value.ToString(CultureInfo.InvariantCulture)).AppendLine();
            builder.AppendLine();
        }

        private static void Gauge(StringBuilder builder, string name, string help, long value)
        {
            builder.Append("# HELP ").Append(name).Append(" ").Append(help).AppendLine();
            builder.Append("# TYPE ").Append(name).AppendLine(" gauge");
            builder.Append(name).Append(" ").Append(value.ToString(CultureInfo.InvariantCulture)).AppendLine();
            builder.AppendLine();
        }
    }
}
