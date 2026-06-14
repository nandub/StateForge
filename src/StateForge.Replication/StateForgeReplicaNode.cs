namespace StateForge.Replication
{
    /// <summary>Represents state forge replica node.</summary>
    public sealed class StateForgeReplicaNode
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the root path.</summary>
        public string RootPath { get; set; }

        /// <summary>Gets or sets the site name.</summary>
        public string SiteName { get; set; }

        /// <summary>Gets or sets the region.</summary>
        public string Region { get; set; }

        /// <summary>Gets or sets the enabled.</summary>
        public bool Enabled { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeReplicaNode"/> class.</summary>
        public StateForgeReplicaNode()
        {
            Enabled = true;
        }
    }
}
