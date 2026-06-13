using System;
using System.Collections.Generic;
using StateForge.Replication;

namespace StateForge.Prometheus
{
    public static class StateForgeReplicaPrometheusCollector
    {
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
