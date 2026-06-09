namespace StateForge.Snapshots
{
    public sealed class StateForgeSnapshotOptions
    {
        public string SourceRootPath { get; set; }

        public string SnapshotRepositoryPath { get; set; }

        public string SnapshotName { get; set; }

        public bool OverwriteExisting { get; set; }

        public StateForgeSnapshotOptions()
        {
            OverwriteExisting = false;
        }
    }
}
