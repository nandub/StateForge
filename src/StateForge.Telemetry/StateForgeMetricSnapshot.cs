using System;

namespace StateForge.Telemetry
{
    /// <summary>Represents a point-in-time snapshot of process-local StateForge counters.</summary>
    public sealed class StateForgeMetricSnapshot
    {
        /// <summary>Gets or sets the number of recorded reads.</summary>
        public long Reads { get; set; }

        /// <summary>Gets or sets the number of recorded writes.</summary>
        public long Writes { get; set; }

        /// <summary>Gets or sets the number of recorded deletions.</summary>
        public long Deletes { get; set; }

        /// <summary>Gets or sets the number of acquired locks.</summary>
        public long LocksAcquired { get; set; }

        /// <summary>Gets or sets the number of lock-contention observations.</summary>
        public long LockContentions { get; set; }

        /// <summary>Gets or sets the number of completed cleanup operations.</summary>
        public long Cleanups { get; set; }

        /// <summary>Gets or sets the number of quarantined files.</summary>
        public long Quarantines { get; set; }

        /// <summary>Gets or sets the number of detected corruption events.</summary>
        public long Corruptions { get; set; }

        /// <summary>Gets or sets the UTC time at which the snapshot was captured.</summary>
        public DateTimeOffset CapturedUtc { get; set; }
    }
}
