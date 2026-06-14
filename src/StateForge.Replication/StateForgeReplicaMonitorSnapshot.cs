using System;
using System.Collections.Generic;

namespace StateForge.Replication
{
    /// <summary>Represents state forge replica monitor snapshot.</summary>
    public sealed class StateForgeReplicaMonitorSnapshot
    {
        /// <summary>Gets or sets the captured utc.</summary>
        public DateTimeOffset CapturedUtc { get; set; }
        /// <summary>Gets or sets the stale threshold.</summary>
        public TimeSpan StaleThreshold { get; set; }
        /// <summary>Gets the replicas.</summary>
        public List<StateForgeReplicaMonitorEntry> Replicas { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeReplicaMonitorSnapshot"/> class.</summary>
        public StateForgeReplicaMonitorSnapshot()
        {
            Replicas = new List<StateForgeReplicaMonitorEntry>();
        }
    }
}
