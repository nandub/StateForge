using System;
using System.Threading;

namespace StateForge.Telemetry
{
    /// <summary>Maintains process-local counters for common StateForge operations.</summary>
    /// <remarks>Counter updates are thread-safe. Values are cumulative until <see cref="Reset"/> is called.</remarks>
    public static class StateForgeMetrics
    {
        private static long reads;
        private static long writes;
        private static long deletes;
        private static long locksAcquired;
        private static long lockContentions;
        private static long cleanups;
        private static long quarantines;
        private static long corruptions;

        /// <summary>Records one successful session read.</summary>
        public static void RecordRead()
        {
            Interlocked.Increment(ref reads);
            StateForgeEventSource.Log.SessionRead();
        }

        /// <summary>Records one session write.</summary>
        public static void RecordWrite()
        {
            Interlocked.Increment(ref writes);
            StateForgeEventSource.Log.SessionWritten();
        }

        /// <summary>Records one session deletion.</summary>
        public static void RecordDelete()
        {
            Interlocked.Increment(ref deletes);
            StateForgeEventSource.Log.SessionDeleted();
        }

        /// <summary>Records one acquired session lock.</summary>
        public static void RecordLockAcquired()
        {
            Interlocked.Increment(ref locksAcquired);
            StateForgeEventSource.Log.LockAcquired();
        }

        /// <summary>Records one lock-contention observation.</summary>
        public static void RecordLockContention()
        {
            Interlocked.Increment(ref lockContentions);
            StateForgeEventSource.Log.LockContention();
        }

        /// <summary>Records one completed cleanup operation.</summary>
        public static void RecordCleanup()
        {
            Interlocked.Increment(ref cleanups);
            StateForgeEventSource.Log.CleanupCompleted();
        }

        /// <summary>Records one quarantined file.</summary>
        public static void RecordQuarantine()
        {
            Interlocked.Increment(ref quarantines);
            StateForgeEventSource.Log.FileQuarantined();
        }

        /// <summary>Records one detected corruption event.</summary>
        public static void RecordCorruption()
        {
            Interlocked.Increment(ref corruptions);
            StateForgeEventSource.Log.CorruptionDetected();
        }

        /// <summary>Captures an atomic point-in-time view of all process-local counters.</summary>
        /// <returns>The current metric values and capture timestamp.</returns>
        /// <example>
        /// Capture counters for a custom health or metrics endpoint:
        /// <code language="csharp">
        /// StateForgeMetricSnapshot snapshot = StateForgeMetrics.Snapshot();
        /// Console.WriteLine($"reads={snapshot.Reads} writes={snapshot.Writes}");
        /// </code>
        /// </example>
        public static StateForgeMetricSnapshot Snapshot()
        {
            StateForgeMetricSnapshot snapshot = new StateForgeMetricSnapshot();
            snapshot.Reads = Interlocked.Read(ref reads);
            snapshot.Writes = Interlocked.Read(ref writes);
            snapshot.Deletes = Interlocked.Read(ref deletes);
            snapshot.LocksAcquired = Interlocked.Read(ref locksAcquired);
            snapshot.LockContentions = Interlocked.Read(ref lockContentions);
            snapshot.Cleanups = Interlocked.Read(ref cleanups);
            snapshot.Quarantines = Interlocked.Read(ref quarantines);
            snapshot.Corruptions = Interlocked.Read(ref corruptions);
            snapshot.CapturedUtc = DateTimeOffset.UtcNow;
            return snapshot;
        }

        /// <summary>Resets all process-local counters to zero.</summary>
        public static void Reset()
        {
            Interlocked.Exchange(ref reads, 0);
            Interlocked.Exchange(ref writes, 0);
            Interlocked.Exchange(ref deletes, 0);
            Interlocked.Exchange(ref locksAcquired, 0);
            Interlocked.Exchange(ref lockContentions, 0);
            Interlocked.Exchange(ref cleanups, 0);
            Interlocked.Exchange(ref quarantines, 0);
            Interlocked.Exchange(ref corruptions, 0);
        }
    }
}
