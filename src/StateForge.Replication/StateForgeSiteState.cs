using System;

namespace StateForge.Replication
{
    public sealed class StateForgeSiteState
    {
        public string Version { get; set; }

        public string SiteName { get; set; }

        public string Region { get; set; }

        public StateForgeSiteRole Role { get; set; }

        public string RootPath { get; set; }

        public bool Enabled { get; set; }

        public bool Healthy { get; set; }

        public bool PromotionEligible { get; set; }

        public DateTimeOffset LastHeartbeatUtc { get; set; }

        public DateTimeOffset LastRecoveryPointUtc { get; set; }

        public string LastError { get; set; }

        public StateForgeSiteState()
        {
            Version = "1";
            Enabled = true;
            Healthy = true;
            PromotionEligible = true;
        }
    }
}
