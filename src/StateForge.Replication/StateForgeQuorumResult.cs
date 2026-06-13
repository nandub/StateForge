using System.Collections.Generic;

namespace StateForge.Replication
{
    public sealed class StateForgeQuorumResult
    {
        public int TotalVotingMembers { get; set; }

        public int AvailableVotes { get; set; }

        public int RequiredVotes { get; set; }

        public bool HasQuorum { get; set; }

        public string CandidateName { get; set; }

        public bool CandidateFound { get; set; }

        public bool CandidateEligible { get; set; }

        public List<string> Reasons { get; private set; }

        public StateForgeQuorumResult()
        {
            Reasons = new List<string>();
        }
    }
}
