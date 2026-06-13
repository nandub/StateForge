using System.Collections.Generic;
using StateForge.Replication;

namespace StateForge.Snapshots
{
    public sealed class StateForgeFailoverOptions
    {
        public string PrimaryRootPath { get; set; }

        public string NewPrimaryRootPath { get; set; }

        public List<string> ReplicaRootPaths { get; private set; }

        public bool Force { get; set; }

        public bool RequirePromotionFence { get; set; }

        public StateForgePromotionFenceOptions PromotionFence { get; set; }

        public StateForgeFailoverOptions()
        {
            ReplicaRootPaths = new List<string>();
        }
    }
}
