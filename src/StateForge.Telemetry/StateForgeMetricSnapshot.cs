using System;

namespace StateForge.Telemetry
{
    public sealed class StateForgeMetricSnapshot
    {
        public long Reads { get; set; }

        public long Writes { get; set; }

        public long Deletes { get; set; }

        public long LocksAcquired { get; set; }

        public long LockContentions { get; set; }

        public long Cleanups { get; set; }

        public long Quarantines { get; set; }

        public long Corruptions { get; set; }

        public DateTimeOffset CapturedUtc { get; set; }
    }
}
