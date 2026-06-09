namespace StateForge.Snapshots
{
    public sealed class StateForgeSnapshotScheduleOptions
    {
        public string SourceRootPath { get; set; }

        public string SnapshotRepositoryPath { get; set; }

        public int RetainLast { get; set; }

        public StateForgeSnapshotScheduleOptions()
        {
            RetainLast = 5;
        }
    }
}
