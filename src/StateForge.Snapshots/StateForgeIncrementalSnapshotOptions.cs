namespace StateForge.Snapshots
{
    /// <summary>Represents state forge incremental snapshot options.</summary>
    public sealed class StateForgeIncrementalSnapshotOptions
    {
        /// <summary>Gets or sets the source root path.</summary>
        public string SourceRootPath { get; set; }
        /// <summary>Gets or sets the snapshot repository path.</summary>
        public string SnapshotRepositoryPath { get; set; }
        /// <summary>Gets or sets the snapshot name.</summary>
        public string SnapshotName { get; set; }
        /// <summary>Gets or sets the parent snapshot name.</summary>
        public string ParentSnapshotName { get; set; }
        /// <summary>Gets or sets the overwrite existing.</summary>
        public bool OverwriteExisting { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeIncrementalSnapshotOptions"/> class.</summary>
        public StateForgeIncrementalSnapshotOptions()
        {
            OverwriteExisting = false;
        }
    }
}
