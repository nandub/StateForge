namespace StateForge.Replication
{
    public sealed class StateForgeQuorumPolicy
    {
        public int MinimumVotes { get; set; }

        public bool RequireCandidateVote { get; set; }

        public bool RequireCandidateAvailable { get; set; }

        public StateForgeQuorumPolicy()
        {
            RequireCandidateVote = true;
            RequireCandidateAvailable = true;
        }
    }
}
