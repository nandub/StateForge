namespace StateForge.Snapshots
{
    /// <summary>Represents state forge incremental snapshot entry.</summary>
    public sealed class StateForgeIncrementalSnapshotEntry
    {
        /// <summary>Gets or sets the relative path.</summary>
        public string RelativePath { get; set; }
        /// <summary>Gets or sets the action.</summary>
        public string Action { get; set; }
        /// <summary>Gets or sets the length.</summary>
        public long Length { get; set; }
        /// <summary>Gets or sets the last write utc.</summary>
        public string LastWriteUtc { get; set; }
    }
}
