namespace StateForge.Replication
{
    public sealed class StateForgeReplicaNode
    {
        public string Name { get; set; }

        public string RootPath { get; set; }

        public bool Enabled { get; set; }

        public StateForgeReplicaNode()
        {
            Enabled = true;
        }
    }
}
