using System;

namespace StateForge.Core
{
    /// <summary>Describes the outcome of an entry lookup with lock acquisition.</summary>
    public sealed class StateForgeLockResult
    {
        /// <summary>Gets or sets a value indicating whether the entry was found.</summary>
        public bool Found { get; set; }
        /// <summary>Gets or sets a value indicating whether another request holds the lock.</summary>
        public bool LockedByOtherRequest { get; set; }
        /// <summary>Gets or sets the age of a lock held by another request.</summary>
        public TimeSpan LockAge { get; set; }
        /// <summary>Gets or sets the current lock fencing token.</summary>
        public long LockId { get; set; }
        /// <summary>Gets or sets the entry when lock acquisition succeeds.</summary>
        public StateForgeEntry Entry { get; set; }

        /// <summary>Creates a result for a missing entry.</summary>
        /// <returns>A result whose <see cref="Found"/> value is <see langword="false"/>.</returns>
        public static StateForgeLockResult NotFound()
        {
            return new StateForgeLockResult { Found = false };
        }

        /// <summary>Creates a result for an entry locked by another request.</summary>
        /// <param name="lockAge">The current age of the lock.</param>
        /// <param name="lockId">The current lock fencing token.</param>
        /// <returns>A result describing the competing lock.</returns>
        public static StateForgeLockResult Locked(TimeSpan lockAge, long lockId)
        {
            return new StateForgeLockResult { Found = true, LockedByOtherRequest = true, LockAge = lockAge, LockId = lockId };
        }

        /// <summary>Creates a result for a successfully acquired lock.</summary>
        /// <param name="entry">The locked entry.</param>
        /// <returns>A result containing <paramref name="entry"/> and its lock token.</returns>
        public static StateForgeLockResult Acquired(StateForgeEntry entry)
        {
            return new StateForgeLockResult { Found = true, LockedByOtherRequest = false, LockId = entry == null ? 0 : entry.LockId, Entry = entry };
        }
    }
}
