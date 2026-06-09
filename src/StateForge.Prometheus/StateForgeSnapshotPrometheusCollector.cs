using StateForge.Performance;

namespace StateForge.Prometheus
{
    public static class StateForgeSnapshotPrometheusCollector
    {
        public static string CollectTextFromSnapshotFile(string snapshotPath)
        {
            StateForgeStoreSnapshot snapshot = StateForgeStoreSnapshotCache.Read(snapshotPath);
            return StateForgeSnapshotPrometheusFormatter.Format(snapshot);
        }

        public static string CaptureAndCollectText(string rootPath, string snapshotPath)
        {
            StateForgeStoreSnapshot snapshot = StateForgeStoreSnapshotCache.CaptureAndWrite(rootPath, snapshotPath);
            return StateForgeSnapshotPrometheusFormatter.Format(snapshot);
        }
    }
}
