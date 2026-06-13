namespace StateForge.Replication
{
    public sealed class StateForgeReplicaCatchUpOptions
    {
        public string PrimaryRootPath { get; set; }

        public string ReplicaRootPath { get; set; }

        public string ReplicaName { get; set; }

        public bool DryRun { get; set; }

        public bool DeleteExtraReplicaFiles { get; set; }

        public StateForgeReplicaCatchUpOptions()
        {
            DryRun = true;
            DeleteExtraReplicaFiles = false;
        }
    }
}
