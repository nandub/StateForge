using System.Collections.Generic;

namespace StateForge.Snapshots
{
    /// <summary>Represents state forge incremental snapshot manifest.</summary>
    public sealed class StateForgeIncrementalSnapshotManifest
    {
        /// <summary>Gets or sets the version.</summary>
        public string Version { get; set; }
        /// <summary>Gets or sets the snapshot name.</summary>
        public string SnapshotName { get; set; }
        /// <summary>Gets or sets the snapshot type.</summary>
        public string SnapshotType { get; set; }
        /// <summary>Gets or sets the parent snapshot name.</summary>
        public string ParentSnapshotName { get; set; }
        /// <summary>Gets or sets the created utc.</summary>
        public string CreatedUtc { get; set; }
        /// <summary>Gets or sets the source root path.</summary>
        public string SourceRootPath { get; set; }
        /// <summary>Gets or sets the snapshot path.</summary>
        public string SnapshotPath { get; set; }
        /// <summary>Gets or sets the files added.</summary>
        public int FilesAdded { get; set; }
        /// <summary>Gets or sets the files modified.</summary>
        public int FilesModified { get; set; }
        /// <summary>Gets or sets the files deleted.</summary>
        public int FilesDeleted { get; set; }
        /// <summary>Gets the entries.</summary>
        public List<StateForgeIncrementalSnapshotEntry> Entries { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeIncrementalSnapshotManifest"/> class.</summary>
        public StateForgeIncrementalSnapshotManifest()
        {
            Version = "0.27.0";
            SnapshotType = "Incremental";
            Entries = new List<StateForgeIncrementalSnapshotEntry>();
        }
    }
}
