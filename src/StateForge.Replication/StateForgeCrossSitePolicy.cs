using System;

namespace StateForge.Replication
{
    public sealed class StateForgeCrossSitePolicy
    {
        public bool RequireDifferentRegion { get; set; }

        public bool RequireHealthyTarget { get; set; }

        public TimeSpan MaximumHeartbeatAge { get; set; }

        public TimeSpan MaximumRecoveryPointAge { get; set; }

        public StateForgeCrossSitePolicy()
        {
            RequireDifferentRegion = true;
            RequireHealthyTarget = true;
            MaximumHeartbeatAge = TimeSpan.FromMinutes(5);
            MaximumRecoveryPointAge = TimeSpan.FromMinutes(15);
        }
    }
}
