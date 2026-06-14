using System.Collections.Generic;

namespace StateForge.Replication
{
    /// <summary>Represents state forge replication plan.</summary>
    public sealed class StateForgeReplicationPlan
    {
        /// <summary>Gets or sets the primary root path.</summary>
        public string PrimaryRootPath { get; set; }

        /// <summary>Gets or sets the primary sessions path.</summary>
        public string PrimarySessionsPath { get; set; }

        /// <summary>Gets the targets.</summary>
        public List<StateForgeReplicationTarget> Targets { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeReplicationPlan"/> class.</summary>
        public StateForgeReplicationPlan()
        {
            Targets = new List<StateForgeReplicationTarget>();
        }
    }
}
