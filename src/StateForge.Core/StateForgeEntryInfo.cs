using System;

namespace StateForge.Core
{
    /// <summary>Describes a stored entry without returning its payload.</summary>
    public sealed class StateForgeEntryInfo
    {
        /// <summary>Gets or sets the logical entry key.</summary>
        public string Key { get; set; }
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
        /// <summary>Gets or sets the physical record path.</summary>
        public string PhysicalPath { get; set; }
        /// <summary>Gets or sets the stored payload length in bytes.</summary>
        public long PayloadLength { get; set; }
        /// <summary>Gets or sets a value indicating whether the entry has expired.</summary>
        public bool Expired { get; set; }
        /// <summary>Gets or sets a value indicating whether the payload is compressed.</summary>
        public bool Compressed { get; set; }
        /// <summary>Gets or sets a value indicating whether the payload is encrypted.</summary>
        public bool Encrypted { get; set; }
        /// <summary>Gets or sets a value indicating whether the payload uses AES encryption.</summary>
        public bool AesEncrypted { get; set; }
    }
}
