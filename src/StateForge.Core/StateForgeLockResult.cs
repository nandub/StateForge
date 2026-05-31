using System;

namespace StateForge.Core
{
    public sealed class StateForgeLockResult
    {
        public bool Found { get; set; }
        public bool LockedByOtherRequest { get; set; }
        public TimeSpan LockAge { get; set; }
        public long LockId { get; set; }
        public StateForgeEntry Entry { get; set; }

        public static StateForgeLockResult NotFound()
        {
            return new StateForgeLockResult { Found = false };
        }

        public static StateForgeLockResult Locked(TimeSpan lockAge, long lockId)
        {
            return new StateForgeLockResult { Found = true, LockedByOtherRequest = true, LockAge = lockAge, LockId = lockId };
        }

        public static StateForgeLockResult Acquired(StateForgeEntry entry)
        {
            return new StateForgeLockResult { Found = true, LockedByOtherRequest = false, LockId = entry == null ? 0 : entry.LockId, Entry = entry };
        }
    }
}
