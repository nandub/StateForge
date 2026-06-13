using System;
using System.Collections.Generic;

namespace StateForge.Replication
{
    public sealed class StateForgeReplicaMonitorSnapshot
    {
        public DateTimeOffset CapturedUtc { get; set; }
        public TimeSpan StaleThreshold { get; set; }
        public List<StateForgeReplicaMonitorEntry> Replicas { get; private set; }

        public StateForgeReplicaMonitorSnapshot()
        {
            Replicas = new List<StateForgeReplicaMonitorEntry>();
        }
    }
}
