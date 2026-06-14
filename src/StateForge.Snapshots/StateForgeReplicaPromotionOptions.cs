using StateForge.Replication;

namespace StateForge.Snapshots
{
    /// <summary>Represents state forge replica promotion options.</summary>
    public sealed class StateForgeReplicaPromotionOptions
    {
        /// <summary>Gets or sets the replica root path.</summary>
        public string ReplicaRootPath { get; set; }

        /// <summary>Gets or sets the new primary root path.</summary>
        public string NewPrimaryRootPath { get; set; }

        /// <summary>Gets or sets the overwrite existing.</summary>
        public bool OverwriteExisting { get; set; }

        /// <summary>Gets or sets the require promotion fence.</summary>
        public bool RequirePromotionFence { get; set; }

        /// <summary>Gets or sets the promotion fence.</summary>
        public StateForgePromotionFenceOptions PromotionFence { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeReplicaPromotionOptions"/> class.</summary>
        public StateForgeReplicaPromotionOptions()
        {
            OverwriteExisting = false;
        }
    }
}
