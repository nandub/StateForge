using System;
using System.Threading;

namespace StateForge.Telemetry
{
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

        public static void RecordRead()
        {
            Interlocked.Increment(ref reads);
            StateForgeEventSource.Log.SessionRead();
        }

        public static void RecordWrite()
        {
            Interlocked.Increment(ref writes);
            StateForgeEventSource.Log.SessionWritten();
        }

        public static void RecordDelete()
        {
            Interlocked.Increment(ref deletes);
            StateForgeEventSource.Log.SessionDeleted();
        }

        public static void RecordLockAcquired()
        {
            Interlocked.Increment(ref locksAcquired);
            StateForgeEventSource.Log.LockAcquired();
        }

        public static void RecordLockContention()
        {
            Interlocked.Increment(ref lockContentions);
            StateForgeEventSource.Log.LockContention();
        }

        public static void RecordCleanup()
        {
            Interlocked.Increment(ref cleanups);
            StateForgeEventSource.Log.CleanupCompleted();
        }

        public static void RecordQuarantine()
        {
            Interlocked.Increment(ref quarantines);
            StateForgeEventSource.Log.FileQuarantined();
        }

        public static void RecordCorruption()
        {
            Interlocked.Increment(ref corruptions);
            StateForgeEventSource.Log.CorruptionDetected();
        }

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
