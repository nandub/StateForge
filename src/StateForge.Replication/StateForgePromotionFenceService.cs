using System;

namespace StateForge.Replication
{
    public sealed class StateForgePromotionFenceService
    {
        public StateForgePromotionFenceResult Acquire(StateForgePromotionFenceOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            StateForgePromotionFenceResult result = Validate(options);
            if (result.Reasons.Count > 0)
            {
                return result;
            }

            DateTimeOffset now = options.EvaluationUtc.HasValue
                ? options.EvaluationUtc.Value
                : DateTimeOffset.UtcNow;
            string path = StateForgePrimaryLeaseStore.GetPath(options.LeaseRootPath);

            using (StateForgePrimaryLeaseLock.Acquire(path))
            {
                StateForgePrimaryLease existing = StateForgePrimaryLeaseStore.Read(options.LeaseRootPath);
                if (existing != null &&
                    !string.Equals(existing.ClusterName, options.ClusterName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    result.Reasons.Add("Primary lease belongs to a different cluster.");
                    return result;
                }

                bool stale = existing != null && IsStale(existing, now);
                result.ExistingPrimaryStale = stale;

                if (existing != null &&
                    !stale &&
                    !string.Equals(existing.PrimaryName, options.CandidateName.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    result.Lease = existing;
                    result.Reasons.Add(
                        "Promotion is fenced by active primary '" + existing.PrimaryName +
                        "' through " + existing.ExpiresUtc.ToString("o") + ".");
                    return result;
                }

                if (existing != null &&
                    !stale &&
                    !string.Equals(existing.LeaseId, options.LeaseId, StringComparison.Ordinal))
                {
                    result.Lease = existing;
                    result.Reasons.Add("Active primary lease requires its ownership token.");
                    return result;
                }

                StateForgePrimaryLease lease;
                if (existing != null && !stale)
                {
                    lease = existing;
                    lease.RenewedUtc = now;
                    lease.ExpiresUtc = now.Add(options.LeaseDuration);
                }
                else
                {
                    lease = new StateForgePrimaryLease();
                    lease.ClusterName = options.ClusterName.Trim();
                    lease.PrimaryName = options.CandidateName.Trim();
                    lease.LeaseId = Guid.NewGuid().ToString("N");
                    lease.Epoch = existing == null ? 1 : checked(existing.Epoch + 1);
                    lease.AcquiredUtc = now;
                    lease.RenewedUtc = now;
                    lease.ExpiresUtc = now.Add(options.LeaseDuration);
                }

                StateForgePrimaryLeaseStore.WriteLocked(options.LeaseRootPath, lease);
                result.Lease = lease;
                result.Acquired = true;
                return result;
            }
        }

        public StateForgePromotionFenceResult Renew(
            string leaseRootPath,
            string clusterName,
            string primaryName,
            string leaseId,
            TimeSpan leaseDuration,
            DateTimeOffset? renewalUtc)
        {
            StateForgePromotionFenceResult result = new StateForgePromotionFenceResult();
            if (string.IsNullOrWhiteSpace(clusterName) ||
                string.IsNullOrWhiteSpace(primaryName) ||
                string.IsNullOrWhiteSpace(leaseId) ||
                leaseDuration <= TimeSpan.Zero)
            {
                result.Reasons.Add("Cluster, primary, lease token, and positive duration are required.");
                return result;
            }

            DateTimeOffset now = renewalUtc.HasValue ? renewalUtc.Value : DateTimeOffset.UtcNow;
            string path = StateForgePrimaryLeaseStore.GetPath(leaseRootPath);
            using (StateForgePrimaryLeaseLock.Acquire(path))
            {
                StateForgePrimaryLease lease = StateForgePrimaryLeaseStore.Read(leaseRootPath);
                result.Lease = lease;
                if (lease == null)
                {
                    result.Reasons.Add("Primary lease does not exist.");
                    return result;
                }

                if (!string.Equals(lease.ClusterName, clusterName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(lease.PrimaryName, primaryName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(lease.LeaseId, leaseId.Trim(), StringComparison.Ordinal))
                {
                    result.Reasons.Add("Primary lease ownership token does not match.");
                    return result;
                }

                if (IsStale(lease, now))
                {
                    result.ExistingPrimaryStale = true;
                    result.Reasons.Add("Primary lease has expired and cannot be renewed.");
                    return result;
                }

                lease.RenewedUtc = now;
                lease.ExpiresUtc = now.Add(leaseDuration);
                StateForgePrimaryLeaseStore.WriteLocked(leaseRootPath, lease);
                result.Acquired = true;
                return result;
            }
        }

        public static bool IsStale(StateForgePrimaryLease lease, DateTimeOffset evaluationUtc)
        {
            return lease == null || lease.ExpiresUtc <= evaluationUtc;
        }

        private static StateForgePromotionFenceResult Validate(StateForgePromotionFenceOptions options)
        {
            StateForgePromotionFenceResult result = new StateForgePromotionFenceResult();
            if (string.IsNullOrWhiteSpace(options.LeaseRootPath))
            {
                result.Reasons.Add("Lease root path is required.");
            }

            if (string.IsNullOrWhiteSpace(options.ClusterName))
            {
                result.Reasons.Add("Cluster name is required.");
            }

            if (string.IsNullOrWhiteSpace(options.CandidateName))
            {
                result.Reasons.Add("Promotion candidate name is required.");
            }

            if (options.LeaseDuration <= TimeSpan.Zero)
            {
                result.Reasons.Add("Lease duration must be positive.");
            }

            if (options.QuorumResult == null ||
                !options.QuorumResult.HasQuorum ||
                !options.QuorumResult.CandidateEligible)
            {
                result.Reasons.Add("Promotion requires quorum and an eligible candidate.");
            }
            else if (!string.Equals(
                options.QuorumResult.CandidateName,
                options.CandidateName,
                StringComparison.OrdinalIgnoreCase))
            {
                result.Reasons.Add("Quorum result does not match the promotion candidate.");
            }

            return result;
        }
    }
}
