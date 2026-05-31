namespace StateForge.Core
{
    public sealed class StateForgeStoreStats
    {
        public int TotalSessions { get; set; }
        public int ExpiredSessions { get; set; }
        public int LockedSessions { get; set; }
        public int CompressedSessions { get; set; }
        public int EncryptedSessions { get; set; }
        public int AesEncryptedSessions { get; set; }
        public long TotalPayloadBytes { get; set; }
        public long AveragePayloadBytes { get; set; }
    }
}
