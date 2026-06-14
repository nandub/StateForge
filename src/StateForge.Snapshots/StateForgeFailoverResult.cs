using StateForge.Replication;

namespace StateForge.Snapshots
{
    /// <summary>Represents state forge failover result.</summary>
    public sealed class StateForgeFailoverResult
    {
        /// <summary>Gets or sets the success.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the primary healthy.</summary>
        public bool PrimaryHealthy { get; set; }

        /// <summary>Gets or sets the promoted replica root path.</summary>
        public string PromotedReplicaRootPath { get; set; }

        /// <summary>Gets or sets the marker path.</summary>
        public string MarkerPath { get; set; }

        /// <summary>Gets or sets the errors.</summary>
        public int Errors { get; set; }

        /// <summary>Gets or sets the promotion fence.</summary>
        public StateForgePromotionFenceResult PromotionFence { get; set; }

        /// <summary>Gets or sets the cross site policy.</summary>
        public StateForgeCrossSiteResult CrossSitePolicy { get; set; }
    }
}
