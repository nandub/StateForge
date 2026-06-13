using System;

namespace StateForge.Replication
{
    public sealed class StateForgeReplicaSyncState
    {
        public string Version { get; set; }
        public string ReplicaName { get; set; }
        public string ReplicaRootPath { get; set; }
        public DateTimeOffset? LastAttemptUtc { get; set; }
        public DateTimeOffset? LastSuccessfulSyncUtc { get; set; }
        public long CatchUpOperations { get; set; }
        public long FailedSyncs { get; set; }
        public string LastError { get; set; }

        public StateForgeReplicaSyncState()
        {
            Version = "1";
        }
    }
}
