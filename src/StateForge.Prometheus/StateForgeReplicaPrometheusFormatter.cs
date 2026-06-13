using System;
using System.Globalization;
using System.Text;
using StateForge.Replication;

namespace StateForge.Prometheus
{
    public static class StateForgeReplicaPrometheusFormatter
    {
        public static string Format(StateForgeReplicaMonitorSnapshot snapshot)
        {
            StringBuilder builder = new StringBuilder();
            AppendHeader(builder, "stateforge_replica_lag_seconds", "Current replica lag in seconds.", "gauge");
            AppendHeader(builder, "stateforge_replica_healthy", "Whether the replica is healthy and within the stale threshold.", "gauge");
            AppendHeader(builder, "stateforge_replica_last_sync_timestamp", "Unix timestamp of the last successful replica sync.", "gauge");
            AppendHeader(builder, "stateforge_replica_catchup_operations_total", "Total replica catch-up operations.", "counter");
            AppendHeader(builder, "stateforge_replica_failed_syncs_total", "Total failed replica sync operations.", "counter");

            if (snapshot == null)
            {
                return builder.ToString();
            }

            for (int i = 0; i < snapshot.Replicas.Count; i++)
            {
                StateForgeReplicaMonitorEntry replica = snapshot.Replicas[i];
                string labels = "{replica=\"" + EscapeLabel(replica.ReplicaName) +
                    "\",root=\"" + EscapeLabel(replica.ReplicaRootPath) + "\"}";
                long lastSync = replica.LastSuccessfulSyncUtc.HasValue
                    ? replica.LastSuccessfulSyncUtc.Value.ToUnixTimeSeconds()
                    : 0;

                Sample(builder, "stateforge_replica_lag_seconds", labels, replica.LagSeconds);
                Sample(builder, "stateforge_replica_healthy", labels, replica.Healthy ? 1 : 0);
                Sample(builder, "stateforge_replica_last_sync_timestamp", labels, lastSync);
                Sample(builder, "stateforge_replica_catchup_operations_total", labels, replica.CatchUpOperations);
                Sample(builder, "stateforge_replica_failed_syncs_total", labels, replica.FailedSyncs);
            }

            return builder.ToString();
        }

        private static void AppendHeader(StringBuilder builder, string name, string help, string type)
        {
            builder.Append("# HELP ").Append(name).Append(" ").Append(help).AppendLine();
            builder.Append("# TYPE ").Append(name).Append(" ").Append(type).AppendLine();
        }

        private static void Sample(StringBuilder builder, string name, string labels, long value)
        {
            builder.Append(name).Append(labels).Append(" ")
                .Append(value.ToString(CultureInfo.InvariantCulture)).AppendLine();
        }

        private static void Sample(StringBuilder builder, string name, string labels, double value)
        {
            builder.Append(name).Append(labels).Append(" ")
                .Append(value.ToString("0.###", CultureInfo.InvariantCulture)).AppendLine();
        }

        private static string EscapeLabel(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n");
        }
    }
}
