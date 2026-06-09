namespace StateForge.Prometheus
{
    public sealed class StateForgePrometheusSnapshot
    {
        public long Reads { get; set; }
        public long Writes { get; set; }
        public long Deletes { get; set; }
        public long LocksAcquired { get; set; }
        public long LockContentions { get; set; }
        public long Cleanups { get; set; }
        public long Quarantines { get; set; }
        public long Corruptions { get; set; }
        public int SessionsActive { get; set; }
        public int SessionsExpired { get; set; }
        public int SessionsLocked { get; set; }
        public int SessionsCompressed { get; set; }
        public int SessionsEncrypted { get; set; }
        public int SessionsAesEncrypted { get; set; }
        public long TotalPayloadBytes { get; set; }
    }
}
