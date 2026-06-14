namespace StateForge.Replication
{
    public sealed class StateForgeReplicationManifestEntry
    {
        public string RelativePath { get; set; }

        public long SourceLength { get; set; }

        public string SourceLastWriteUtc { get; set; }

        public string ReplicaName { get; set; }

        public string SiteName { get; set; }

        public string Region { get; set; }

        public string DestinationPath { get; set; }

        public string Action { get; set; }

        public string Reason { get; set; }
    }
}
