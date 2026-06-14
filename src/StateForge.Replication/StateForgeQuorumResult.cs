using System.Collections.Generic;

namespace StateForge.Replication
{
    /// <summary>Represents state forge quorum result.</summary>
    public sealed class StateForgeQuorumResult
    {
        /// <summary>Gets or sets the total voting members.</summary>
        public int TotalVotingMembers { get; set; }

        /// <summary>Gets or sets the available votes.</summary>
        public int AvailableVotes { get; set; }

        /// <summary>Gets or sets the required votes.</summary>
        public int RequiredVotes { get; set; }

        /// <summary>Gets or sets the has quorum.</summary>
        public bool HasQuorum { get; set; }

        /// <summary>Gets or sets the candidate name.</summary>
        public string CandidateName { get; set; }

        /// <summary>Gets or sets the candidate found.</summary>
        public bool CandidateFound { get; set; }

        /// <summary>Gets or sets the candidate eligible.</summary>
        public bool CandidateEligible { get; set; }

        /// <summary>Gets the reasons.</summary>
        public List<string> Reasons { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeQuorumResult"/> class.</summary>
        public StateForgeQuorumResult()
        {
            Reasons = new List<string>();
        }
    }
}
