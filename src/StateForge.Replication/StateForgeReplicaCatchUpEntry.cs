namespace StateForge.Replication
{
    public sealed class StateForgeReplicaCatchUpEntry
    {
        public string RelativePath { get; set; }

        public string Action { get; set; }

        public long PrimaryLength { get; set; }

        public long ReplicaLength { get; set; }
    }
}
