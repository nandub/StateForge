using System;
using StateForge.Performance;

namespace StateForge.Prometheus
{
    internal static class StateForgeSnapshotAge
    {
        public static double GetAgeSeconds(StateForgeStoreSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.CapturedUtc))
            {
                return 0;
            }

            DateTimeOffset captured;

            if (!DateTimeOffset.TryParse(snapshot.CapturedUtc, out captured))
            {
                return 0;
            }

            double seconds = (DateTimeOffset.UtcNow - captured.ToUniversalTime()).TotalSeconds;
            return seconds < 0 ? 0 : seconds;
        }
    }
}
