using StateForge.Replication;

namespace StateForge.Snapshots
{
    /// <summary>Represents state forge replica promotion result.</summary>
    public sealed class StateForgeReplicaPromotionResult
    {
        /// <summary>Gets or sets the success.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the files copied.</summary>
        public int FilesCopied { get; set; }

        /// <summary>Gets or sets the files skipped.</summary>
        public int FilesSkipped { get; set; }

        /// <summary>Gets or sets the errors.</summary>
        public int Errors { get; set; }

        /// <summary>Gets or sets the promotion marker path.</summary>
        public string PromotionMarkerPath { get; set; }

        /// <summary>Gets or sets the promotion fence.</summary>
        public StateForgePromotionFenceResult PromotionFence { get; set; }
    }
}
