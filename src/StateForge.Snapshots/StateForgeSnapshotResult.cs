namespace StateForge.Snapshots
{
    /// <summary>Represents state forge snapshot result.</summary>
    public sealed class StateForgeSnapshotResult
    {
        /// <summary>Gets or sets the success.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the snapshot name.</summary>
        public string SnapshotName { get; set; }

        /// <summary>Gets or sets the snapshot path.</summary>
        public string SnapshotPath { get; set; }

        /// <summary>Gets or sets the manifest path.</summary>
        public string ManifestPath { get; set; }

        /// <summary>Gets or sets the files copied.</summary>
        public int FilesCopied { get; set; }

        /// <summary>Gets or sets the files skipped.</summary>
        public int FilesSkipped { get; set; }

        /// <summary>Gets or sets the errors.</summary>
        public int Errors { get; set; }
    }
}
