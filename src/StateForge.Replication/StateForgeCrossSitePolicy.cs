using System;

namespace StateForge.Replication
{
    /// <summary>Represents state forge cross site policy.</summary>
    public sealed class StateForgeCrossSitePolicy
    {
        /// <summary>Gets or sets the require different region.</summary>
        public bool RequireDifferentRegion { get; set; }

        /// <summary>Gets or sets the require healthy target.</summary>
        public bool RequireHealthyTarget { get; set; }

        /// <summary>Gets or sets the maximum heartbeat age.</summary>
        public TimeSpan MaximumHeartbeatAge { get; set; }

        /// <summary>Gets or sets the maximum recovery point age.</summary>
        public TimeSpan MaximumRecoveryPointAge { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeCrossSitePolicy"/> class.</summary>
        public StateForgeCrossSitePolicy()
        {
            RequireDifferentRegion = true;
            RequireHealthyTarget = true;
            MaximumHeartbeatAge = TimeSpan.FromMinutes(5);
            MaximumRecoveryPointAge = TimeSpan.FromMinutes(15);
        }
    }
}
