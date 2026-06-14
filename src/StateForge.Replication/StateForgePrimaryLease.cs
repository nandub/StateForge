using System;

namespace StateForge.Replication
{
    /// <summary>Represents state forge primary lease.</summary>
    public sealed class StateForgePrimaryLease
    {
        /// <summary>Gets or sets the version.</summary>
        public string Version { get; set; }

        /// <summary>Gets or sets the cluster name.</summary>
        public string ClusterName { get; set; }

        /// <summary>Gets or sets the primary name.</summary>
        public string PrimaryName { get; set; }

        /// <summary>Gets or sets the lease id.</summary>
        public string LeaseId { get; set; }

        /// <summary>Gets or sets the epoch.</summary>
        public long Epoch { get; set; }

        /// <summary>Gets or sets the acquired utc.</summary>
        public DateTimeOffset AcquiredUtc { get; set; }

        /// <summary>Gets or sets the renewed utc.</summary>
        public DateTimeOffset RenewedUtc { get; set; }

        /// <summary>Gets or sets the expires utc.</summary>
        public DateTimeOffset ExpiresUtc { get; set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgePrimaryLease"/> class.</summary>
        public StateForgePrimaryLease()
        {
            Version = "1";
        }
    }
}
