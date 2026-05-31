using System;

namespace StateForge.Core
{
    public sealed class StateForgeEntryInfo
    {
        public string Key { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset UpdatedUtc { get; set; }
        public DateTimeOffset ExpiresUtc { get; set; }
        public bool Locked { get; set; }
        public long LockId { get; set; }
        public string PhysicalPath { get; set; }
        public long PayloadLength { get; set; }
        public bool Expired { get; set; }
        public bool Compressed { get; set; }
        public bool Encrypted { get; set; }
        public bool AesEncrypted { get; set; }
    }
}
