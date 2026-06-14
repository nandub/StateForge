namespace StateForge.Prometheus
{
    /// <summary>Represents state forge prometheus snapshot.</summary>
    public sealed class StateForgePrometheusSnapshot
    {
        /// <summary>Gets or sets the reads.</summary>
        public long Reads { get; set; }
        /// <summary>Gets or sets the writes.</summary>
        public long Writes { get; set; }
        /// <summary>Gets or sets the deletes.</summary>
        public long Deletes { get; set; }
        /// <summary>Gets or sets the locks acquired.</summary>
        public long LocksAcquired { get; set; }
        /// <summary>Gets or sets the lock contentions.</summary>
        public long LockContentions { get; set; }
        /// <summary>Gets or sets the cleanups.</summary>
        public long Cleanups { get; set; }
        /// <summary>Gets or sets the quarantines.</summary>
        public long Quarantines { get; set; }
        /// <summary>Gets or sets the corruptions.</summary>
        public long Corruptions { get; set; }
        /// <summary>Gets or sets the sessions active.</summary>
        public int SessionsActive { get; set; }
        /// <summary>Gets or sets the sessions expired.</summary>
        public int SessionsExpired { get; set; }
        /// <summary>Gets or sets the sessions locked.</summary>
        public int SessionsLocked { get; set; }
        /// <summary>Gets or sets the sessions compressed.</summary>
        public int SessionsCompressed { get; set; }
        /// <summary>Gets or sets the sessions encrypted.</summary>
        public int SessionsEncrypted { get; set; }
        /// <summary>Gets or sets the sessions aes encrypted.</summary>
        public int SessionsAesEncrypted { get; set; }
        /// <summary>Gets or sets the total payload bytes.</summary>
        public long TotalPayloadBytes { get; set; }
    }
}
