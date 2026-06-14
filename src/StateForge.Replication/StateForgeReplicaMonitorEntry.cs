using System;

namespace StateForge.Replication
{
    /// <summary>Represents state forge replica monitor entry.</summary>
    public sealed class StateForgeReplicaMonitorEntry
    {
        /// <summary>Gets or sets the replica name.</summary>
        public string ReplicaName { get; set; }
        /// <summary>Gets or sets the replica root path.</summary>
        public string ReplicaRootPath { get; set; }
        /// <summary>Gets or sets the last attempt utc.</summary>
        public DateTimeOffset? LastAttemptUtc { get; set; }
        /// <summary>Gets or sets the last successful sync utc.</summary>
        public DateTimeOffset? LastSuccessfulSyncUtc { get; set; }
        /// <summary>Gets or sets the lag seconds.</summary>
        public double LagSeconds { get; set; }
        /// <summary>Gets or sets the healthy.</summary>
        public bool Healthy { get; set; }
        /// <summary>Gets or sets the stale.</summary>
        public bool Stale { get; set; }
        /// <summary>Gets or sets the catch up operations.</summary>
        public long CatchUpOperations { get; set; }
        /// <summary>Gets or sets the failed syncs.</summary>
        public long FailedSyncs { get; set; }
        /// <summary>Gets or sets the last error.</summary>
        public string LastError { get; set; }
    }
}
