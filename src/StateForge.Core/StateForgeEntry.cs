using System;

namespace StateForge.Core
{
    /// <summary>Represents a stored state entry and its lock and expiration metadata.</summary>
    public sealed class StateForgeEntry
    {
        /// <summary>Gets or sets the logical entry key.</summary>
        public string Key { get; set; }
        /// <summary>Gets or sets the binary payload.</summary>
        public byte[] Value { get; set; }
        /// <summary>Gets or sets the UTC creation time.</summary>
        public DateTimeOffset CreatedUtc { get; set; }
        /// <summary>Gets or sets the UTC time of the most recent update.</summary>
        public DateTimeOffset UpdatedUtc { get; set; }
        /// <summary>Gets or sets the UTC expiration time.</summary>
        public DateTimeOffset ExpiresUtc { get; set; }
        /// <summary>Gets or sets a value indicating whether the entry is locked.</summary>
        public bool Locked { get; set; }
        /// <summary>Gets or sets the lock fencing token.</summary>
        public long LockId { get; set; }
        /// <summary>Gets or sets the UTC time at which the current lock was acquired.</summary>
        public DateTimeOffset? LockDateUtc { get; set; }

        /// <summary>Determines whether the entry has expired at a specified UTC time.</summary>
        /// <param name="utcNow">The UTC time to compare with <see cref="ExpiresUtc"/>.</param>
        /// <returns><see langword="true"/> when the expiration time is at or before <paramref name="utcNow"/>.</returns>
        public bool IsExpired(DateTimeOffset utcNow)
        {
            return ExpiresUtc <= utcNow;
        }
    }
}
