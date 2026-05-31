using System;

namespace StateForge.Core
{
    public sealed class StateForgeEntry
    {
        public string Key { get; set; }
        public byte[] Value { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset UpdatedUtc { get; set; }
        public DateTimeOffset ExpiresUtc { get; set; }
        public bool Locked { get; set; }
        public long LockId { get; set; }
        public DateTimeOffset? LockDateUtc { get; set; }

        public bool IsExpired(DateTimeOffset utcNow)
        {
            return ExpiresUtc <= utcNow;
        }
    }
}
