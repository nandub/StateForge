using System;

namespace StateForge.Replication
{
    /// <summary>Represents state forge promotion fence options.</summary>
    public sealed class StateForgePromotionFenceOptions
    {
        /// <summary>Gets or sets the lease root path.</summary>
        public string LeaseRootPath { get; set; }

        /// <summary>Gets or sets the cluster name.</summary>
        public string ClusterName { get; set; }

        /// <summary>Gets or sets the candidate name.</summary>
        public string CandidateName { get; set; }

        /// <summary>Gets or sets the lease id.</summary>
        public string LeaseId { get; set; }

        /// <summary>Gets or sets the quorum result.</summary>
        public StateForgeQuorumResult QuorumResult { get; set; }

        /// <summary>Gets or sets the lease duration.</summary>
        public TimeSpan LeaseDuration { get; set; }

        /// <summary>Gets or sets the evaluation utc.</summary>
        public DateTimeOffset? EvaluationUtc { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgePromotionFenceOptions"/> class.</summary>
        public StateForgePromotionFenceOptions()
        {
            LeaseDuration = TimeSpan.FromSeconds(30);
        }
    }
}
