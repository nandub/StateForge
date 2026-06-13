using System;
using System.Collections.Generic;
using StateForge.Replication;

namespace StateForge.QuorumTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                List<StateForgeClusterMember> members = CreateThreeMemberCluster();

                StateForgeQuorumResult healthy = StateForgeQuorumEvaluator.Evaluate(
                    members,
                    new StateForgeQuorumPolicy(),
                    "replica-a");
                Require(healthy.TotalVotingMembers == 3, "Voting member count mismatch.");
                Require(healthy.RequiredVotes == 2, "Majority vote count mismatch.");
                Require(healthy.AvailableVotes == 3, "Available vote count mismatch.");
                Require(healthy.HasQuorum, "Healthy cluster should have quorum.");
                Require(healthy.CandidateEligible, "Healthy replica should be promotion eligible.");

                members[2].Available = false;
                StateForgeQuorumResult oneUnavailable = StateForgeQuorumEvaluator.Evaluate(
                    members,
                    new StateForgeQuorumPolicy(),
                    "replica-a");
                Require(oneUnavailable.HasQuorum, "Three-member cluster should tolerate one unavailable voter.");

                members[1].Available = false;
                StateForgeQuorumResult quorumLost = StateForgeQuorumEvaluator.Evaluate(
                    members,
                    new StateForgeQuorumPolicy(),
                    "replica-a");
                Require(!quorumLost.HasQuorum, "Cluster with one of three votes should lose quorum.");
                Require(!quorumLost.CandidateEligible, "Candidate must not be eligible without quorum.");

                members = CreateThreeMemberCluster();
                members[1].Voting = false;
                StateForgeQuorumResult nonVoting = StateForgeQuorumEvaluator.Evaluate(
                    members,
                    new StateForgeQuorumPolicy(),
                    "replica-a");
                Require(nonVoting.HasQuorum, "Remaining two voters should satisfy majority.");
                Require(!nonVoting.CandidateEligible, "Non-voting candidate should be rejected by default.");

                StateForgeQuorumPolicy relaxedPolicy = new StateForgeQuorumPolicy();
                relaxedPolicy.RequireCandidateVote = false;
                StateForgeQuorumResult relaxed = StateForgeQuorumEvaluator.Evaluate(
                    members,
                    relaxedPolicy,
                    "replica-a");
                Require(relaxed.CandidateEligible, "Policy should allow a non-voting candidate.");

                StateForgeQuorumPolicy explicitPolicy = new StateForgeQuorumPolicy();
                explicitPolicy.MinimumVotes = 3;
                StateForgeQuorumResult explicitVotes = StateForgeQuorumEvaluator.Evaluate(
                    members,
                    explicitPolicy,
                    "replica-b");
                Require(!explicitVotes.HasQuorum, "Explicit three-vote policy should not have quorum.");

                StateForgeQuorumResult primaryCandidate = StateForgeQuorumEvaluator.Evaluate(
                    CreateThreeMemberCluster(),
                    new StateForgeQuorumPolicy(),
                    "primary");
                Require(!primaryCandidate.CandidateEligible, "Primary role must not be a promotion candidate.");

                StateForgeQuorumResult normalizedCandidate = StateForgeQuorumEvaluator.Evaluate(
                    CreateThreeMemberCluster(),
                    new StateForgeQuorumPolicy(),
                    " replica-a ");
                Require(normalizedCandidate.CandidateEligible, "Candidate names should be trimmed.");

                VerifyInvalidConfigurations();

                Console.WriteLine("PASS: majority quorum calculation");
                Console.WriteLine("PASS: unavailable voter tolerance");
                Console.WriteLine("PASS: quorum loss blocks promotion");
                Console.WriteLine("PASS: candidate vote policy");
                Console.WriteLine("PASS: explicit minimum vote policy");
                Console.WriteLine("PASS: replica role promotion eligibility");
                Console.WriteLine("PASS: invalid cluster configuration rejection");
                Console.WriteLine("PASS: no automatic leader election");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static List<StateForgeClusterMember> CreateThreeMemberCluster()
        {
            List<StateForgeClusterMember> members = new List<StateForgeClusterMember>();
            members.Add(new StateForgeClusterMember
            {
                Name = "primary",
                Role = StateForgeClusterMemberRole.Primary
            });
            members.Add(new StateForgeClusterMember
            {
                Name = "replica-a",
                Role = StateForgeClusterMemberRole.Replica
            });
            members.Add(new StateForgeClusterMember
            {
                Name = "replica-b",
                Role = StateForgeClusterMemberRole.Replica
            });
            return members;
        }

        private static void VerifyInvalidConfigurations()
        {
            bool duplicateRejected = false;
            try
            {
                List<StateForgeClusterMember> members = CreateThreeMemberCluster();
                members.Add(new StateForgeClusterMember { Name = " replica-a " });
                StateForgeQuorumEvaluator.Evaluate(members, null, "replica-a");
            }
            catch (ArgumentException)
            {
                duplicateRejected = true;
            }

            Require(duplicateRejected, "Duplicate member names should be rejected.");

            bool negativeVotesRejected = false;
            try
            {
                StateForgeQuorumPolicy policy = new StateForgeQuorumPolicy();
                policy.MinimumVotes = -1;
                StateForgeQuorumEvaluator.Evaluate(CreateThreeMemberCluster(), policy, "replica-a");
            }
            catch (ArgumentOutOfRangeException)
            {
                negativeVotesRejected = true;
            }

            Require(negativeVotesRejected, "Negative minimum votes should be rejected.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
