namespace StateForge.Replication
{
    /// <summary>Represents state forge replication manifest entry.</summary>
    public sealed class StateForgeReplicationManifestEntry
    {
        /// <summary>Gets or sets the relative path.</summary>
        public string RelativePath { get; set; }

        /// <summary>Gets or sets the source length.</summary>
        public long SourceLength { get; set; }

        /// <summary>Gets or sets the source last write utc.</summary>
        public string SourceLastWriteUtc { get; set; }

        /// <summary>Gets or sets the replica name.</summary>
        public string ReplicaName { get; set; }

        /// <summary>Gets or sets the site name.</summary>
        public string SiteName { get; set; }

        /// <summary>Gets or sets the region.</summary>
        public string Region { get; set; }

        /// <summary>Gets or sets the destination path.</summary>
        public string DestinationPath { get; set; }

        /// <summary>Gets or sets the action.</summary>
        public string Action { get; set; }

        /// <summary>Gets or sets the reason.</summary>
        public string Reason { get; set; }
    }
}
