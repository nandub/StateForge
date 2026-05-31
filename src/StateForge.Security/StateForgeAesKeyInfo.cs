using System;

namespace StateForge.Security
{
    public sealed class StateForgeAesKeyInfo
    {
        public string KeyId { get; set; }

        public string KeyBase64 { get; set; }

        public DateTimeOffset CreatedUtc { get; set; }

        public DateTimeOffset? NotBeforeUtc { get; set; }

        public DateTimeOffset? RetiredUtc { get; set; }

        public bool IsRetired
        {
            get
            {
                return RetiredUtc.HasValue;
            }
        }
    }
}
