using System;

namespace StateForge.Replication
{
    /// <summary>Provides state forge cross site evaluator operations.</summary>
    public static class StateForgeCrossSiteEvaluator
    {
        /// <summary>Evaluates whether a recovery site satisfies the cross-site promotion policy.</summary>
        public static StateForgeCrossSiteResult Evaluate(
            StateForgeSiteState source,
            StateForgeSiteState target,
            StateForgeCrossSitePolicy policy,
            StateForgeQuorumResult quorum,
            string candidateName,
            DateTimeOffset evaluationUtc)
        {
            if (policy == null)
            {
                policy = new StateForgeCrossSitePolicy();
            }

            if (policy.MaximumHeartbeatAge <= TimeSpan.Zero ||
                policy.MaximumRecoveryPointAge <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("policy", "Site freshness thresholds must be positive.");
            }

            StateForgeCrossSiteResult result = new StateForgeCrossSiteResult();
            result.SourceSiteName = source == null ? string.Empty : source.SiteName;
            result.TargetSiteName = target == null ? string.Empty : target.SiteName;
            result.TargetRootPath = target == null ? string.Empty : target.RootPath;
            result.CandidateName = string.IsNullOrWhiteSpace(candidateName)
                ? string.Empty
                : candidateName.Trim();

            if (source == null || target == null)
            {
                result.Reasons.Add("Source and target site state are required.");
                return result;
            }

            if (!source.Enabled || source.Role != StateForgeSiteRole.Primary)
            {
                result.Reasons.Add("Source site must be an enabled primary site.");
            }

            if (string.IsNullOrWhiteSpace(source.SiteName) ||
                string.IsNullOrWhiteSpace(source.Region) ||
                string.IsNullOrWhiteSpace(source.RootPath) ||
                !System.IO.Path.IsPathRooted(source.RootPath))
            {
                result.Reasons.Add("Source site identity, region, and root path are required.");
            }

            if (!target.Enabled || target.Role != StateForgeSiteRole.Recovery)
            {
                result.Reasons.Add("Target site must be an enabled recovery site.");
            }

            if (string.IsNullOrWhiteSpace(target.SiteName) ||
                string.IsNullOrWhiteSpace(target.Region) ||
                string.IsNullOrWhiteSpace(target.RootPath) ||
                !System.IO.Path.IsPathRooted(target.RootPath))
            {
                result.Reasons.Add("Target site identity, region, and root path are required.");
            }

            if (string.Equals(source.SiteName, target.SiteName, StringComparison.OrdinalIgnoreCase))
            {
                result.Reasons.Add("Source and target sites must be different.");
            }

            if (policy.RequireDifferentRegion &&
                string.Equals(source.Region, target.Region, StringComparison.OrdinalIgnoreCase))
            {
                result.Reasons.Add("Cross-site policy requires a different target region.");
            }

            if (policy.RequireHealthyTarget && !target.Healthy)
            {
                result.Reasons.Add("Target site is unhealthy.");
            }

            if (!target.PromotionEligible)
            {
                result.Reasons.Add("Target site is not promotion eligible.");
            }

            if (!string.IsNullOrWhiteSpace(target.LastError))
            {
                result.Reasons.Add("Target site reported an error: " + target.LastError);
            }

            if (target.LastHeartbeatUtc > evaluationUtc ||
                evaluationUtc - target.LastHeartbeatUtc > policy.MaximumHeartbeatAge)
            {
                result.Reasons.Add("Target site heartbeat is stale or in the future.");
            }

            if (target.LastRecoveryPointUtc > evaluationUtc ||
                evaluationUtc - target.LastRecoveryPointUtc > policy.MaximumRecoveryPointAge)
            {
                result.Reasons.Add("Target site recovery point is stale or in the future.");
            }

            if (quorum == null ||
                !quorum.HasQuorum ||
                !quorum.CandidateEligible ||
                !string.Equals(quorum.CandidateName, candidateName, StringComparison.OrdinalIgnoreCase))
            {
                result.Reasons.Add("Cross-site failover requires quorum for the exact candidate.");
            }

            result.Eligible = result.Reasons.Count == 0;
            return result;
        }
    }
}
