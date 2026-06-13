using System.Collections.Generic;

namespace StateForge.Snapshots
{
    public sealed class StateForgeIncrementalSnapshotManifest
    {
        public string Version { get; set; }
        public string SnapshotName { get; set; }
        public string SnapshotType { get; set; }
        public string ParentSnapshotName { get; set; }
        public string CreatedUtc { get; set; }
        public string SourceRootPath { get; set; }
        public string SnapshotPath { get; set; }
        public int FilesAdded { get; set; }
        public int FilesModified { get; set; }
        public int FilesDeleted { get; set; }
        public List<StateForgeIncrementalSnapshotEntry> Entries { get; private set; }

        public StateForgeIncrementalSnapshotManifest()
        {
            Version = "0.27.0";
            SnapshotType = "Incremental";
            Entries = new List<StateForgeIncrementalSnapshotEntry>();
        }
    }
}
