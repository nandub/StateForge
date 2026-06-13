using System;

namespace StateForge.Replication
{
    public sealed class StateForgeReplicaMonitorEntry
    {
        public string ReplicaName { get; set; }
        public string ReplicaRootPath { get; set; }
        public DateTimeOffset? LastAttemptUtc { get; set; }
        public DateTimeOffset? LastSuccessfulSyncUtc { get; set; }
        public double LagSeconds { get; set; }
        public bool Healthy { get; set; }
        public bool Stale { get; set; }
        public long CatchUpOperations { get; set; }
        public long FailedSyncs { get; set; }
        public string LastError { get; set; }
    }
}
