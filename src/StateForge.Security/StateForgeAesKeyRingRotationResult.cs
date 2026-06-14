using System;

namespace StateForge.Security
{
    /// <summary>Summarizes a persisted AES key-ring rotation.</summary>
    public sealed class StateForgeAesKeyRingRotationResult
    {
        /// <summary>Gets or sets the identifier of the key that was current before rotation.</summary>
        public string PreviousKeyId { get; set; }

        /// <summary>Gets or sets the identifier of the new current key.</summary>
        public string CurrentKeyId { get; set; }

        /// <summary>Gets or sets the number of keys retained after rotation.</summary>
        public int KeyCount { get; set; }

        /// <summary>Gets or sets the UTC time at which rotation completed.</summary>
        public DateTimeOffset RotatedUtc { get; set; }
    }
}
