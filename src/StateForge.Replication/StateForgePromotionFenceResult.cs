using System.Collections.Generic;

namespace StateForge.Replication
{
    /// <summary>Represents state forge promotion fence result.</summary>
    public sealed class StateForgePromotionFenceResult
    {
        /// <summary>Gets or sets the acquired.</summary>
        public bool Acquired { get; set; }

        /// <summary>Gets or sets the existing primary stale.</summary>
        public bool ExistingPrimaryStale { get; set; }

        /// <summary>Gets or sets the lease.</summary>
        public StateForgePrimaryLease Lease { get; set; }

        /// <summary>Gets the reasons.</summary>
        public List<string> Reasons { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgePromotionFenceResult"/> class.</summary>
        public StateForgePromotionFenceResult()
        {
            Reasons = new List<string>();
        }
    }
}
