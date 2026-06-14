using System;
using System.IO;

namespace StateForge.Replication
{
    /// <summary>Provides state forge witness evaluator operations.</summary>
    public static class StateForgeWitnessEvaluator
    {
        /// <summary>Evaluates witness freshness and candidate voting state.</summary>
        public static StateForgeWitnessHealthEntry Evaluate(
            StateForgeWitnessNode witness,
            string candidateName,
            TimeSpan staleThreshold)
        {
            return Evaluate(witness, candidateName, staleThreshold, DateTimeOffset.UtcNow);
        }

        /// <summary>Evaluates witness state using an explicit evaluation time.</summary>
        public static StateForgeWitnessHealthEntry Evaluate(
            StateForgeWitnessNode witness,
            string candidateName,
            TimeSpan staleThreshold,
            DateTimeOffset capturedUtc)
        {
            if (witness == null)
            {
                throw new ArgumentNullException("witness");
            }

            if (staleThreshold < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("staleThreshold");
            }

            StateForgeWitnessHealthEntry result = new StateForgeWitnessHealthEntry();
            result.WitnessName = string.IsNullOrWhiteSpace(witness.Name) ? "witness" : witness.Name.Trim();
            result.WitnessRootPath = string.IsNullOrWhiteSpace(witness.RootPath)
                ? string.Empty
                : Path.GetFullPath(witness.RootPath);
            result.CandidateName = string.IsNullOrWhiteSpace(candidateName)
                ? string.Empty
                : candidateName.Trim();

            if (result.CandidateName.Length == 0)
            {
                result.Reasons.Add("A witness vote candidate name is required.");
                return result;
            }

            if (!witness.Enabled)
            {
                result.Reasons.Add("Witness is disabled.");
                return result;
            }

            if (string.IsNullOrWhiteSpace(witness.RootPath))
            {
                result.Reasons.Add("Witness root path is required.");
                return result;
            }

            try
            {
                StateForgeWitnessState state = StateForgeWitnessStateStore.Read(witness.RootPath);
                if (state == null)
                {
                    result.Reasons.Add("Witness state is missing.");
                    return result;
                }

                result.LastHeartbeatUtc = state.LastHeartbeatUtc;
                result.AgeSeconds = Math.Max(0, capturedUtc.Subtract(state.LastHeartbeatUtc).TotalSeconds);
                result.VoteGranted = state.VoteGranted;
                bool fresh = result.AgeSeconds <= staleThreshold.TotalSeconds;
                bool identityMatches = string.Equals(
                    state.WitnessName,
                    result.WitnessName,
                    StringComparison.OrdinalIgnoreCase);

                if (!fresh)
                {
                    result.Reasons.Add("Witness heartbeat is stale.");
                }

                if (!identityMatches)
                {
                    result.Reasons.Add("Witness state identity does not match the configured witness.");
                }

                if (!string.IsNullOrWhiteSpace(state.LastError))
                {
                    result.Reasons.Add("Witness reported an error: " + state.LastError);
                }

                result.Healthy =
                    fresh &&
                    identityMatches &&
                    string.IsNullOrWhiteSpace(state.LastError);

                if (!state.VoteGranted)
                {
                    result.Reasons.Add("Witness did not grant a vote.");
                }
                else if (!string.Equals(
                    state.CandidateName,
                    result.CandidateName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    result.Reasons.Add("Witness vote targets a different candidate.");
                }

                result.VoteCounted =
                    witness.Voting &&
                    result.Healthy &&
                    state.VoteGranted &&
                    string.Equals(
                        state.CandidateName,
                        result.CandidateName,
                        StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                result.Reasons.Add(ex.GetType().Name + ": " + ex.Message);
            }

            return result;
        }

        /// <summary>Performs the to cluster member operation.</summary>
        public static StateForgeClusterMember ToClusterMember(
            StateForgeWitnessNode witness,
            StateForgeWitnessHealthEntry health)
        {
            if (witness == null)
            {
                throw new ArgumentNullException("witness");
            }

            if (health == null)
            {
                throw new ArgumentNullException("health");
            }

            return new StateForgeClusterMember
            {
                Name = health.WitnessName,
                Role = StateForgeClusterMemberRole.Witness,
                Enabled = witness.Enabled,
                Voting = witness.Voting,
                Available = health.VoteCounted,
                PromotionEligible = false
            };
        }
    }
}
