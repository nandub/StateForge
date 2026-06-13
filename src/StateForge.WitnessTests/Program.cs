using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using StateForge.Replication;

namespace StateForge.WitnessTests
{
    internal static class Program
    {
        private static int Main()
        {
            string root = Path.Combine(Path.GetTempPath(), "StateForgeWitnessTests");
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                string witnessRoot = Path.Combine(root, "witness-a");
                DateTimeOffset now = new DateTimeOffset(2026, 6, 13, 18, 0, 0, TimeSpan.Zero);
                StateForgeWitnessNode witness = new StateForgeWitnessNode
                {
                    Name = "witness-a",
                    RootPath = witnessRoot
                };

                StateForgeWitnessStateStore.Write(
                    witnessRoot,
                    new StateForgeWitnessState
                    {
                        WitnessName = "witness-a",
                        LastHeartbeatUtc = now.AddSeconds(-10),
                        CandidateName = "replica-a",
                        VoteGranted = true,
                        LastError = string.Empty
                    });

                StateForgeWitnessState persisted = StateForgeWitnessStateStore.Read(witnessRoot);
                Require(persisted.WitnessName == "witness-a", "Witness name was not persisted.");
                Require(persisted.VoteGranted, "Witness vote was not persisted.");

                StateForgeWitnessHealthEntry healthy = StateForgeWitnessEvaluator.Evaluate(
                    witness,
                    "replica-a",
                    TimeSpan.FromSeconds(30),
                    now);
                Require(healthy.Healthy, "Fresh witness should be healthy.");
                Require(healthy.VoteCounted, "Fresh matching witness vote should count.");

                StateForgeWitnessHealthEntry wrongCandidate = StateForgeWitnessEvaluator.Evaluate(
                    witness,
                    "replica-b",
                    TimeSpan.FromSeconds(30),
                    now);
                Require(!wrongCandidate.VoteCounted, "Vote for a different candidate must not count.");

                StateForgeWitnessHealthEntry stale = StateForgeWitnessEvaluator.Evaluate(
                    witness,
                    "replica-a",
                    TimeSpan.FromSeconds(5),
                    now);
                Require(!stale.Healthy, "Stale witness should be unhealthy.");
                Require(!stale.VoteCounted, "Stale witness vote must not count.");

                StateForgeWitnessHealthEntry missingCandidate = StateForgeWitnessEvaluator.Evaluate(
                    witness,
                    string.Empty,
                    TimeSpan.FromSeconds(30),
                    now);
                Require(!missingCandidate.VoteCounted, "Witness vote must require a candidate.");

                VerifyQuorumIntegration(witness, healthy);
                VerifyMissingAndCorruptState(root, now);
                VerifyIdentityMismatch(witnessRoot, now);
                VerifyWitnessCannotBePromoted(witness, healthy);

                Console.WriteLine("PASS: witness state persistence");
                Console.WriteLine("PASS: fresh witness health");
                Console.WriteLine("PASS: stale witness rejection");
                Console.WriteLine("PASS: candidate-specific witness vote");
                Console.WriteLine("PASS: witness vote restores quorum");
                Console.WriteLine("PASS: missing and corrupt witness state");
                Console.WriteLine("PASS: witness identity validation");
                Console.WriteLine("PASS: witness promotion rejection");
                Console.WriteLine("PASS: no automatic failover integration");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        private static void VerifyIdentityMismatch(string witnessRoot, DateTimeOffset now)
        {
            StateForgeWitnessHealthEntry result = StateForgeWitnessEvaluator.Evaluate(
                new StateForgeWitnessNode
                {
                    Name = "different-witness",
                    RootPath = witnessRoot
                },
                "replica-a",
                TimeSpan.FromSeconds(30),
                now);
            Require(!result.Healthy, "Mismatched witness identity should be unhealthy.");
            Require(!result.VoteCounted, "Mismatched witness identity vote must not count.");
        }

        private static void VerifyQuorumIntegration(
            StateForgeWitnessNode witness,
            StateForgeWitnessHealthEntry health)
        {
            List<StateForgeClusterMember> members = new List<StateForgeClusterMember>();
            members.Add(new StateForgeClusterMember
            {
                Name = "primary",
                Role = StateForgeClusterMemberRole.Primary,
                Available = false
            });
            members.Add(new StateForgeClusterMember
            {
                Name = "replica-a",
                Role = StateForgeClusterMemberRole.Replica
            });

            StateForgeQuorumPolicy policy = new StateForgeQuorumPolicy();
            policy.MinimumVotes = 2;

            StateForgeQuorumResult withoutWitness =
                StateForgeQuorumEvaluator.Evaluate(members, policy, "replica-a");
            Require(!withoutWitness.HasQuorum, "Two-member cluster should lack two available votes.");

            members.Add(StateForgeWitnessEvaluator.ToClusterMember(witness, health));
            StateForgeQuorumResult withWitness =
                StateForgeQuorumEvaluator.Evaluate(members, policy, "replica-a");
            Require(withWitness.HasQuorum, "Validated witness vote should restore quorum.");
            Require(withWitness.CandidateEligible, "Replica should be eligible with witness quorum.");
        }

        private static void VerifyMissingAndCorruptState(string root, DateTimeOffset now)
        {
            StateForgeWitnessNode missing = new StateForgeWitnessNode
            {
                Name = "missing",
                RootPath = Path.Combine(root, "missing")
            };
            StateForgeWitnessHealthEntry missingHealth = StateForgeWitnessEvaluator.Evaluate(
                missing,
                "replica-a",
                TimeSpan.FromSeconds(30),
                now);
            Require(!missingHealth.Healthy, "Missing witness state should be unhealthy.");

            string corruptRoot = Path.Combine(root, "corrupt");
            Directory.CreateDirectory(corruptRoot);
            File.WriteAllText(
                StateForgeWitnessStateStore.GetPath(corruptRoot),
                "{\"version\":\"1\"",
                new UTF8Encoding(false));
            StateForgeWitnessHealthEntry corruptHealth = StateForgeWitnessEvaluator.Evaluate(
                new StateForgeWitnessNode { Name = "corrupt", RootPath = corruptRoot },
                "replica-a",
                TimeSpan.FromSeconds(30),
                now);
            Require(!corruptHealth.Healthy, "Corrupt witness state should be unhealthy.");
            Require(
                corruptHealth.Reasons[0].StartsWith("InvalidDataException:", StringComparison.Ordinal),
                "Corrupt state should report InvalidDataException.");
        }

        private static void VerifyWitnessCannotBePromoted(
            StateForgeWitnessNode witness,
            StateForgeWitnessHealthEntry health)
        {
            List<StateForgeClusterMember> members = new List<StateForgeClusterMember>();
            members.Add(new StateForgeClusterMember
            {
                Name = "replica-a",
                Role = StateForgeClusterMemberRole.Replica
            });
            members.Add(StateForgeWitnessEvaluator.ToClusterMember(witness, health));

            StateForgeQuorumResult result = StateForgeQuorumEvaluator.Evaluate(
                members,
                new StateForgeQuorumPolicy(),
                "witness-a");
            Require(!result.CandidateEligible, "Witness must never be promotion eligible.");
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
