namespace StateForge.Core
{
    /// <summary>Provides aggregate counts and payload sizes for a StateForge store.</summary>
    public sealed class StateForgeStoreStats
    {
        /// <summary>Gets or sets the total number of session records.</summary>
        public int TotalSessions { get; set; }
        /// <summary>Gets or sets the number of expired session records.</summary>
        public int ExpiredSessions { get; set; }
        /// <summary>Gets or sets the number of locked session records.</summary>
        public int LockedSessions { get; set; }
        /// <summary>Gets or sets the number of compressed session records.</summary>
        public int CompressedSessions { get; set; }
        /// <summary>Gets or sets the number of encrypted session records.</summary>
        public int EncryptedSessions { get; set; }
        /// <summary>Gets or sets the number of AES-encrypted session records.</summary>
        public int AesEncryptedSessions { get; set; }
        /// <summary>Gets or sets the total payload size in bytes.</summary>
        public long TotalPayloadBytes { get; set; }
        /// <summary>Gets or sets the average payload size in bytes.</summary>
        public long AveragePayloadBytes { get; set; }
    }
}
