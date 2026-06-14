namespace StateForge.Replication
{
    /// <summary>Represents state forge quorum policy.</summary>
    public sealed class StateForgeQuorumPolicy
    {
        /// <summary>Gets or sets the minimum votes.</summary>
        public int MinimumVotes { get; set; }

        /// <summary>Gets or sets the require candidate vote.</summary>
        public bool RequireCandidateVote { get; set; }

        /// <summary>Gets or sets the require candidate available.</summary>
        public bool RequireCandidateAvailable { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeQuorumPolicy"/> class.</summary>
        public StateForgeQuorumPolicy()
        {
            RequireCandidateVote = true;
            RequireCandidateAvailable = true;
        }
    }
}
