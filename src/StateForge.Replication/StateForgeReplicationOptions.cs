using System.Collections.Generic;

namespace StateForge.Replication
{
    public sealed class StateForgeReplicationOptions
    {
        public string PrimaryRootPath { get; set; }

        public List<StateForgeReplicaNode> Replicas { get; private set; }

        public bool OverwriteExisting { get; set; }

        public bool DryRun { get; set; }

        public bool DetectConflicts { get; set; }

        public string ManifestPath { get; set; }

        public StateForgeReplicationOptions()
        {
            Replicas = new List<StateForgeReplicaNode>();
            OverwriteExisting = true;
            DryRun = false;
            DetectConflicts = true;
        }
    }
}
