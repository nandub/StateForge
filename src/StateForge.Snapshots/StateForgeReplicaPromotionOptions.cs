using StateForge.Replication;

namespace StateForge.Snapshots
{
    public sealed class StateForgeReplicaPromotionOptions
    {
        public string ReplicaRootPath { get; set; }

        public string NewPrimaryRootPath { get; set; }

        public bool OverwriteExisting { get; set; }

        public bool RequirePromotionFence { get; set; }

        public StateForgePromotionFenceOptions PromotionFence { get; set; }

        public StateForgeReplicaPromotionOptions()
        {
            OverwriteExisting = false;
        }
    }
}
