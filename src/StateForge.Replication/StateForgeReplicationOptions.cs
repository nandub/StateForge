using System.Collections.Generic;

namespace StateForge.Replication
{
    public sealed class StateForgeReplicationOptions
    {
        public string PrimaryRootPath { get; set; }

        public List<StateForgeReplicaNode> Replicas { get; private set; }

        public bool OverwriteExisting { get; set; }

        public StateForgeReplicationOptions()
        {
            Replicas = new List<StateForgeReplicaNode>();
            OverwriteExisting = true;
        }
    }
}
