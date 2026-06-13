using System;

namespace StateForge.Replication
{
    public sealed class StateForgePromotionFenceOptions
    {
        public string LeaseRootPath { get; set; }

        public string ClusterName { get; set; }

        public string CandidateName { get; set; }

        public string LeaseId { get; set; }

        public StateForgeQuorumResult QuorumResult { get; set; }

        public TimeSpan LeaseDuration { get; set; }

        public DateTimeOffset? EvaluationUtc { get; set; }

        public StateForgePromotionFenceOptions()
        {
            LeaseDuration = TimeSpan.FromSeconds(30);
        }
    }
}
