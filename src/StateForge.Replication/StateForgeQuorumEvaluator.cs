using System;
using System.Collections.Generic;

namespace StateForge.Replication
{
    public static class StateForgeQuorumEvaluator
    {
        public static StateForgeQuorumResult Evaluate(
            IEnumerable<StateForgeClusterMember> members,
            StateForgeQuorumPolicy policy,
            string candidateName)
        {
            if (policy == null)
            {
                policy = new StateForgeQuorumPolicy();
            }

            if (policy.MinimumVotes < 0)
            {
                throw new ArgumentOutOfRangeException("policy", "MinimumVotes cannot be negative.");
            }

            StateForgeQuorumResult result = new StateForgeQuorumResult();
            result.CandidateName = string.IsNullOrWhiteSpace(candidateName)
                ? string.Empty
                : candidateName.Trim();

            Dictionary<string, StateForgeClusterMember> membersByName =
                new Dictionary<string, StateForgeClusterMember>(StringComparer.OrdinalIgnoreCase);

            if (members != null)
            {
                foreach (StateForgeClusterMember member in members)
                {
                    if (member == null)
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(member.Name))
                    {
                        throw new ArgumentException("Cluster member names are required.", "members");
                    }

                    string memberName = member.Name.Trim();
                    if (membersByName.ContainsKey(memberName))
                    {
                        throw new ArgumentException(
                            "Cluster member names must be unique: " + memberName,
                            "members");
                    }

                    membersByName.Add(memberName, member);

                    if (member.Enabled && member.Voting)
                    {
                        result.TotalVotingMembers++;
                        if (member.Available)
                        {
                            result.AvailableVotes++;
                        }
                    }
                }
            }

            result.RequiredVotes = policy.MinimumVotes > 0
                ? policy.MinimumVotes
                : (result.TotalVotingMembers / 2) + 1;
            result.HasQuorum =
                result.TotalVotingMembers > 0 &&
                result.AvailableVotes >= result.RequiredVotes;

            if (!result.HasQuorum)
            {
                result.Reasons.Add(
                    "Quorum is unavailable: " + result.AvailableVotes +
                    " of " + result.RequiredVotes + " required votes are available.");
            }

            if (string.IsNullOrWhiteSpace(candidateName))
            {
                result.Reasons.Add("A promotion candidate name is required.");
                return result;
            }

            StateForgeClusterMember candidate;
            if (!membersByName.TryGetValue(result.CandidateName, out candidate))
            {
                result.Reasons.Add("Promotion candidate was not found: " + result.CandidateName);
                return result;
            }

            result.CandidateFound = true;

            if (!candidate.Enabled)
            {
                result.Reasons.Add("Promotion candidate is disabled.");
            }

            if (candidate.Role != StateForgeClusterMemberRole.Replica)
            {
                result.Reasons.Add("Promotion candidate must have the Replica role.");
            }

            if (!candidate.PromotionEligible)
            {
                result.Reasons.Add("Promotion candidate is not marked promotion eligible.");
            }

            if (policy.RequireCandidateAvailable && !candidate.Available)
            {
                result.Reasons.Add("Promotion candidate is unavailable.");
            }

            if (policy.RequireCandidateVote && !candidate.Voting)
            {
                result.Reasons.Add("Promotion candidate is not a voting member.");
            }

            result.CandidateEligible =
                result.HasQuorum &&
                candidate.Enabled &&
                candidate.Role == StateForgeClusterMemberRole.Replica &&
                candidate.PromotionEligible &&
                (!policy.RequireCandidateAvailable || candidate.Available) &&
                (!policy.RequireCandidateVote || candidate.Voting);

            return result;
        }
    }
}
