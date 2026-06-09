namespace StateForge.Snapshots
{
    public sealed class StateForgeSnapshotResult
    {
        public bool Success { get; set; }

        public string SnapshotName { get; set; }

        public string SnapshotPath { get; set; }

        public string ManifestPath { get; set; }

        public int FilesCopied { get; set; }

        public int FilesSkipped { get; set; }

        public int Errors { get; set; }
    }
}
