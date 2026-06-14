namespace StateForge.Replication
{
    /// <summary>Represents state forge replica catch up entry.</summary>
    public sealed class StateForgeReplicaCatchUpEntry
    {
        /// <summary>Gets or sets the relative path.</summary>
        public string RelativePath { get; set; }

        /// <summary>Gets or sets the action.</summary>
        public string Action { get; set; }

        /// <summary>Gets or sets the primary length.</summary>
        public long PrimaryLength { get; set; }

        /// <summary>Gets or sets the replica length.</summary>
        public long ReplicaLength { get; set; }
    }
}
