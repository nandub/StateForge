using StateForge.Performance;

namespace StateForge.Prometheus
{
    /// <summary>Provides state forge snapshot prometheus collector operations.</summary>
    public static class StateForgeSnapshotPrometheusCollector
    {
        /// <summary>Reads a store snapshot file and formats its metrics.</summary>
        public static string CollectTextFromSnapshotFile(string snapshotPath)
        {
            StateForgeStoreSnapshot snapshot = StateForgeStoreSnapshotCache.Read(snapshotPath);
            return StateForgeSnapshotPrometheusFormatter.Format(snapshot);
        }

        /// <summary>Performs the capture and collect text operation.</summary>
        public static string CaptureAndCollectText(string rootPath, string snapshotPath)
        {
            StateForgeStoreSnapshot snapshot = StateForgeStoreSnapshotCache.CaptureAndWrite(rootPath, snapshotPath);
            return StateForgeSnapshotPrometheusFormatter.Format(snapshot);
        }
    }
}
