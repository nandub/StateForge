using System;

namespace StateForge.Security
{
    public sealed class StateForgeAesKeyRingRotationResult
    {
        public string PreviousKeyId { get; set; }

        public string CurrentKeyId { get; set; }

        public int KeyCount { get; set; }

        public DateTimeOffset RotatedUtc { get; set; }
    }
}
