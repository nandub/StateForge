using System.Collections.Generic;
using StateForge.Replication;

namespace StateForge.Snapshots
{
    /// <summary>Represents state forge failover options.</summary>
    public sealed class StateForgeFailoverOptions
    {
        /// <summary>Gets or sets the primary root path.</summary>
        public string PrimaryRootPath { get; set; }

        /// <summary>Gets or sets the new primary root path.</summary>
        public string NewPrimaryRootPath { get; set; }

        /// <summary>Gets the replica root paths.</summary>
        public List<string> ReplicaRootPaths { get; private set; }

        /// <summary>Gets or sets the force.</summary>
        public bool Force { get; set; }

        /// <summary>Gets or sets the require promotion fence.</summary>
        public bool RequirePromotionFence { get; set; }

        /// <summary>Gets or sets the promotion fence.</summary>
        public StateForgePromotionFenceOptions PromotionFence { get; set; }

        /// <summary>Gets or sets the require cross site policy.</summary>
        public bool RequireCrossSitePolicy { get; set; }

        /// <summary>Gets or sets the cross site policy.</summary>
        public StateForgeCrossSiteResult CrossSitePolicy { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeFailoverOptions"/> class.</summary>
        public StateForgeFailoverOptions()
        {
            ReplicaRootPaths = new List<string>();
        }
    }
}
