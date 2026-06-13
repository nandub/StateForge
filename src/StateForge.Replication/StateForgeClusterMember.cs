namespace StateForge.Replication
{
    public sealed class StateForgeClusterMember
    {
        public string Name { get; set; }

        public StateForgeClusterMemberRole Role { get; set; }

        public bool Enabled { get; set; }

        public bool Voting { get; set; }

        public bool Available { get; set; }

        public bool PromotionEligible { get; set; }

        public StateForgeClusterMember()
        {
            Enabled = true;
            Voting = true;
            Available = true;
            PromotionEligible = true;
        }
    }
}
