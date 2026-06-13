using StateForge.Replication;

namespace StateForge.Snapshots
{
    public sealed class StateForgeReplicaPromotionResult
    {
        public bool Success { get; set; }

        public int FilesCopied { get; set; }

        public int FilesSkipped { get; set; }

        public int Errors { get; set; }

        public string PromotionMarkerPath { get; set; }

        public StateForgePromotionFenceResult PromotionFence { get; set; }
    }
}
