using System;
using System.IO;
using System.Text;
using StateForge.FileStore;
using StateForge.Performance;
using StateForge.Prometheus;

namespace StateForge.SnapshotTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeSnapshotTests");
                string snapshotPath = Path.Combine(root, "snapshot", "store.json");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(root);

                StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
                options.RootPath = root;

                StateForgeFileStore store = new StateForgeFileStore(options);
                byte[] payload = Encoding.UTF8.GetBytes("snapshot");

                for (int i = 0; i < 10; i++)
                {
                    store.Set("snapshot-" + i.ToString("D4"), payload, TimeSpan.FromMinutes(10));
                }

                StateForgeStoreSnapshot captured = StateForgeStoreSnapshotCache.CaptureAndWrite(root, snapshotPath);
                Require(captured.TotalSessions == 10, "Snapshot count mismatch.");
                Require(File.Exists(snapshotPath), "Snapshot file missing.");

                StateForgeStoreSnapshot loaded = StateForgeStoreSnapshotCache.Read(snapshotPath);
                Require(loaded.TotalSessions == 10, "Loaded snapshot count mismatch.");

                string prometheus = StateForgeSnapshotPrometheusCollector.CollectTextFromSnapshotFile(snapshotPath);
                Require(prometheus.IndexOf("stateforge_sessions_active 10", StringComparison.Ordinal) >= 0, "Snapshot prometheus active session mismatch.");
                Require(prometheus.IndexOf("stateforge_snapshot_age_seconds", StringComparison.Ordinal) >= 0, "Snapshot age metric missing.");

                Console.WriteLine("PASS: snapshot capture");
                Console.WriteLine("PASS: snapshot read");
                Console.WriteLine("PASS: snapshot prometheus");
                Console.WriteLine("PASS: snapshot age metric");

                Directory.Delete(root, true);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
