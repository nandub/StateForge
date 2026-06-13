using System;

namespace StateForge.Replication
{
    public sealed class StateForgePrimaryLease
    {
        public string Version { get; set; }

        public string ClusterName { get; set; }

        public string PrimaryName { get; set; }

        public string LeaseId { get; set; }

        public long Epoch { get; set; }

        public DateTimeOffset AcquiredUtc { get; set; }

        public DateTimeOffset RenewedUtc { get; set; }

        public DateTimeOffset ExpiresUtc { get; set; }

        public StateForgePrimaryLease()
        {
            Version = "1";
        }
    }
}
