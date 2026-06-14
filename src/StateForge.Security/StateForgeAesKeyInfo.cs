using System;

namespace StateForge.Security
{
    /// <summary>Describes one AES key and its lifecycle timestamps.</summary>
    public sealed class StateForgeAesKeyInfo
    {
        /// <summary>Gets or sets the stable identifier embedded in keyed records.</summary>
        public string KeyId { get; set; }

        /// <summary>Gets or sets the Base64-encoded AES key material.</summary>
        public string KeyBase64 { get; set; }

        /// <summary>Gets or sets the UTC creation time.</summary>
        public DateTimeOffset CreatedUtc { get; set; }

        /// <summary>Gets or sets the earliest UTC time at which the key should be used.</summary>
        public DateTimeOffset? NotBeforeUtc { get; set; }

        /// <summary>Gets or sets the UTC retirement time.</summary>
        public DateTimeOffset? RetiredUtc { get; set; }

        /// <summary>Gets a value indicating whether a retirement time has been assigned.</summary>
        public bool IsRetired
        {
            get
            {
                return RetiredUtc.HasValue;
            }
        }
    }
}
