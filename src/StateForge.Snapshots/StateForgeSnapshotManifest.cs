using System.Collections.Generic;

namespace StateForge.Snapshots
{
    public sealed class StateForgeSnapshotManifest
    {
        public string Version { get; set; }

        public string SnapshotName { get; set; }

        public string CreatedUtc { get; set; }

        public string SourceRootPath { get; set; }

        public string SnapshotPath { get; set; }

        public int FileCount { get; set; }

        public long TotalBytes { get; set; }

        public List<StateForgeSnapshotManifestEntry> Entries { get; private set; }

        public StateForgeSnapshotManifest()
        {
            Version = "0.26.0";
            Entries = new List<StateForgeSnapshotManifestEntry>();
        }
    }
}
