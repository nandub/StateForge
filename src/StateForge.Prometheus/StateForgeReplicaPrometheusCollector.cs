using System;
using System.Collections.Generic;
using StateForge.Replication;

namespace StateForge.Prometheus
{
    /// <summary>Provides state forge replica prometheus collector operations.</summary>
    public static class StateForgeReplicaPrometheusCollector
    {
        /// <summary>Collects and formats metrics for the configured replicas.</summary>
        public static string CollectText(
            IEnumerable<StateForgeReplicaNode> replicas,
            TimeSpan staleThreshold)
        {
            StateForgeReplicaMonitorSnapshot snapshot =
                StateForgeReplicaMonitor.Capture(replicas, staleThreshold);
            return StateForgeReplicaPrometheusFormatter.Format(snapshot);
        }
    }
}
