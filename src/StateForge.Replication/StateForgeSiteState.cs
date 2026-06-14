using System;

namespace StateForge.Replication
{
    /// <summary>Represents state forge site state.</summary>
    public sealed class StateForgeSiteState
    {
        /// <summary>Gets or sets the version.</summary>
        public string Version { get; set; }

        /// <summary>Gets or sets the site name.</summary>
        public string SiteName { get; set; }

        /// <summary>Gets or sets the region.</summary>
        public string Region { get; set; }

        /// <summary>Gets or sets the role.</summary>
        public StateForgeSiteRole Role { get; set; }

        /// <summary>Gets or sets the root path.</summary>
        public string RootPath { get; set; }

        /// <summary>Gets or sets the enabled.</summary>
        public bool Enabled { get; set; }

        /// <summary>Gets or sets the healthy.</summary>
        public bool Healthy { get; set; }

        /// <summary>Gets or sets the promotion eligible.</summary>
        public bool PromotionEligible { get; set; }

        /// <summary>Gets or sets the last heartbeat utc.</summary>
        public DateTimeOffset LastHeartbeatUtc { get; set; }

        /// <summary>Gets or sets the last recovery point utc.</summary>
        public DateTimeOffset LastRecoveryPointUtc { get; set; }

        /// <summary>Gets or sets the last error.</summary>
        public string LastError { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeSiteState"/> class.</summary>
        public StateForgeSiteState()
        {
            Version = "1";
            Enabled = true;
            Healthy = true;
            PromotionEligible = true;
        }
    }
}
