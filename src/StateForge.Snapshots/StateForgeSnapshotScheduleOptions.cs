namespace StateForge.Snapshots
{
    /// <summary>Represents state forge snapshot schedule options.</summary>
    public sealed class StateForgeSnapshotScheduleOptions
    {
        /// <summary>Gets or sets the source root path.</summary>
        public string SourceRootPath { get; set; }

        /// <summary>Gets or sets the snapshot repository path.</summary>
        public string SnapshotRepositoryPath { get; set; }

        /// <summary>Gets or sets the retain last.</summary>
        public int RetainLast { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeSnapshotScheduleOptions"/> class.</summary>
        public StateForgeSnapshotScheduleOptions()
        {
            RetainLast = 5;
        }
    }
}
