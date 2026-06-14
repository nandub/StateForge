using System.Collections.Generic;

namespace StateForge.Snapshots
{
    /// <summary>Represents state forge snapshot manifest.</summary>
    public sealed class StateForgeSnapshotManifest
    {
        /// <summary>Gets or sets the version.</summary>
        public string Version { get; set; }

        /// <summary>Gets or sets the snapshot name.</summary>
        public string SnapshotName { get; set; }

        /// <summary>Gets or sets the created utc.</summary>
        public string CreatedUtc { get; set; }

        /// <summary>Gets or sets the source root path.</summary>
        public string SourceRootPath { get; set; }

        /// <summary>Gets or sets the snapshot path.</summary>
        public string SnapshotPath { get; set; }

        /// <summary>Gets or sets the file count.</summary>
        public int FileCount { get; set; }

        /// <summary>Gets or sets the total bytes.</summary>
        public long TotalBytes { get; set; }

        /// <summary>Gets the entries.</summary>
        public List<StateForgeSnapshotManifestEntry> Entries { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeSnapshotManifest"/> class.</summary>
        public StateForgeSnapshotManifest()
        {
            Version = "0.26.0";
            Entries = new List<StateForgeSnapshotManifestEntry>();
        }
    }
}
