using System.Collections.Generic;

namespace StateForge.Replication
{
    /// <summary>Represents state forge replication options.</summary>
    public sealed class StateForgeReplicationOptions
    {
        /// <summary>Gets or sets the primary root path.</summary>
        public string PrimaryRootPath { get; set; }

        /// <summary>Gets the replicas.</summary>
        public List<StateForgeReplicaNode> Replicas { get; private set; }

        /// <summary>Gets or sets the overwrite existing.</summary>
        public bool OverwriteExisting { get; set; }

        /// <summary>Gets or sets the dry run.</summary>
        public bool DryRun { get; set; }

        /// <summary>Gets or sets the detect conflicts.</summary>
        public bool DetectConflicts { get; set; }

        /// <summary>Gets or sets the manifest path.</summary>
        public string ManifestPath { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeReplicationOptions"/> class.</summary>
        public StateForgeReplicationOptions()
        {
            Replicas = new List<StateForgeReplicaNode>();
            OverwriteExisting = true;
            DryRun = false;
            DetectConflicts = true;
        }
    }
}
