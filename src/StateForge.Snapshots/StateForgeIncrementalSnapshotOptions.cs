namespace StateForge.Snapshots
{
    public sealed class StateForgeIncrementalSnapshotOptions
    {
        public string SourceRootPath { get; set; }
        public string SnapshotRepositoryPath { get; set; }
        public string SnapshotName { get; set; }
        public string ParentSnapshotName { get; set; }
        public bool OverwriteExisting { get; set; }

        public StateForgeIncrementalSnapshotOptions()
        {
            OverwriteExisting = false;
        }
    }
}
