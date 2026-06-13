using System.Collections.Generic;

namespace StateForge.Replication
{
    public sealed class StateForgePromotionFenceResult
    {
        public bool Acquired { get; set; }

        public bool ExistingPrimaryStale { get; set; }

        public StateForgePrimaryLease Lease { get; set; }

        public List<string> Reasons { get; private set; }

        public StateForgePromotionFenceResult()
        {
            Reasons = new List<string>();
        }
    }
}
