namespace StateForge.Replication
{
    public sealed class StateForgeWitnessNode
    {
        public string Name { get; set; }

        public string RootPath { get; set; }

        public bool Enabled { get; set; }

        public bool Voting { get; set; }

        public StateForgeWitnessNode()
        {
            Enabled = true;
            Voting = true;
        }
    }
}
