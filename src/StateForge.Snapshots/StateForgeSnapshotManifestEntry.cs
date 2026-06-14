namespace StateForge.Snapshots
{
    /// <summary>Represents state forge snapshot manifest entry.</summary>
    public sealed class StateForgeSnapshotManifestEntry
    {
        /// <summary>Gets or sets the relative path.</summary>
        public string RelativePath { get; set; }

        /// <summary>Gets or sets the length.</summary>
        public long Length { get; set; }

        /// <summary>Gets or sets the last write utc.</summary>
        public string LastWriteUtc { get; set; }
    }
}
