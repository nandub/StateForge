namespace StateForge.Snapshots
{
    public sealed class StateForgeSnapshotManifestEntry
    {
        public string RelativePath { get; set; }

        public long Length { get; set; }

        public string LastWriteUtc { get; set; }
    }
}
