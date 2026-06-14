namespace StateForge.Replication
{
    /// <summary>Represents state forge cluster member.</summary>
    public sealed class StateForgeClusterMember
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the role.</summary>
        public StateForgeClusterMemberRole Role { get; set; }

        /// <summary>Gets or sets the enabled.</summary>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets the voting.</summary>
        public bool Voting { get; set; }

        /// <summary>Gets or sets the available.</summary>
        public bool Available { get; set; }

        /// <summary>Gets or sets the promotion eligible.</summary>
        public bool PromotionEligible { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeClusterMember"/> class.</summary>
        public StateForgeClusterMember()
        {
            Enabled = true;
            Voting = true;
            Available = true;
            PromotionEligible = true;
        }
    }
}
