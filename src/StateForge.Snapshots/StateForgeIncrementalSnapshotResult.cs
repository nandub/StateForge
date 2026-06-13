namespace StateForge.Snapshots
{
    public sealed class StateForgeIncrementalSnapshotResult
    {
        public bool Success { get; set; }
        public string SnapshotName { get; set; }
        public string SnapshotPath { get; set; }
        public string ManifestPath { get; set; }
        public int FilesAdded { get; set; }
        public int FilesModified { get; set; }
        public int FilesDeleted { get; set; }
        public int FilesCopied { get; set; }
        public int Errors { get; set; }
    }
}
