namespace StateForge.Snapshots
{
    public sealed class StateForgeIncrementalSnapshotEntry
    {
        public string RelativePath { get; set; }
        public string Action { get; set; }
        public long Length { get; set; }
        public string LastWriteUtc { get; set; }
    }
}
