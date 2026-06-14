namespace StateForge.Replication
{
    /// <summary>Represents state forge witness node.</summary>
    public sealed class StateForgeWitnessNode
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the root path.</summary>
        public string RootPath { get; set; }

        /// <summary>Gets or sets the enabled.</summary>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets the voting.</summary>
        public bool Voting { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeWitnessNode"/> class.</summary>
        public StateForgeWitnessNode()
        {
            Enabled = true;
            Voting = true;
        }
    }
}
