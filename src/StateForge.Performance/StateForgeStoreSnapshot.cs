namespace StateForge.Performance
{
    /// <summary>Represents a serializable performance and capacity snapshot of a StateForge store.</summary>
    public sealed class StateForgeStoreSnapshot
    {
        /// <summary>Gets or sets the analyzed store root path.</summary>
        public string RootPath { get; set; }
        /// <summary>Gets or sets the ISO 8601 UTC capture timestamp.</summary>
        public string CapturedUtc { get; set; }
        /// <summary>Gets or sets the total session count.</summary>
        public int TotalSessions { get; set; }
        /// <summary>Gets or sets the expired session count.</summary>
        public int ExpiredSessions { get; set; }
        /// <summary>Gets or sets the locked session count.</summary>
        public int LockedSessions { get; set; }
        /// <summary>Gets or sets the compressed session count.</summary>
        public int CompressedSessions { get; set; }
        /// <summary>Gets or sets the encrypted session count.</summary>
        public int EncryptedSessions { get; set; }
        /// <summary>Gets or sets the AES-encrypted session count.</summary>
        public int AesEncryptedSessions { get; set; }
        /// <summary>Gets or sets the total stored payload bytes.</summary>
        public long TotalPayloadBytes { get; set; }
        /// <summary>Gets or sets the average stored payload bytes per session.</summary>
        public long AveragePayloadBytes { get; set; }
        /// <summary>Gets or sets the elapsed capture time in milliseconds.</summary>
        public long CaptureElapsedMs { get; set; }
    }
}
