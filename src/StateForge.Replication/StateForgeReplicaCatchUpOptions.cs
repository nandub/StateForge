namespace StateForge.Replication
{
    /// <summary>Represents state forge replica catch up options.</summary>
    public sealed class StateForgeReplicaCatchUpOptions
    {
        /// <summary>Gets or sets the primary root path.</summary>
        public string PrimaryRootPath { get; set; }

        /// <summary>Gets or sets the replica root path.</summary>
        public string ReplicaRootPath { get; set; }

        /// <summary>Gets or sets the replica name.</summary>
        public string ReplicaName { get; set; }

        /// <summary>Gets or sets the dry run.</summary>
        public bool DryRun { get; set; }

        /// <summary>Gets or sets the delete extra replica files.</summary>
        public bool DeleteExtraReplicaFiles { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeReplicaCatchUpOptions"/> class.</summary>
        public StateForgeReplicaCatchUpOptions()
        {
            DryRun = true;
            DeleteExtraReplicaFiles = false;
        }
    }
}
