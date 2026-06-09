using System;
using System.IO;
using System.Text;
using StateForge.FileStore;
using StateForge.Performance;

namespace StateForge.PerformanceTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgePerformanceTests");
                string snapshot = Path.Combine(root, "snapshot.json");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(root);

                StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
                options.RootPath = root;

                StateForgeFileStore store = new StateForgeFileStore(options);
                byte[] payload = Encoding.UTF8.GetBytes("performance");

                for (int i = 0; i < 32; i++)
                {
                    store.Set("perf-" + i.ToString("D4"), payload, TimeSpan.FromMinutes(10));
                }

                StateForgeStoreSnapshot captured = StateForgeStoreSnapshotCache.CaptureAndWrite(root, snapshot);
                Require(captured.TotalSessions == 32, "Snapshot session count mismatch.");
                Require(File.Exists(snapshot), "Snapshot file was not written.");

                StateForgeStoreSnapshot loaded = StateForgeStoreSnapshotCache.Read(snapshot);
                Require(loaded.TotalSessions == 32, "Loaded snapshot session count mismatch.");

                StateForgeShardAnalysisResult shard = StateForgeShardAnalyzer.Analyze(root);
                Require(shard.FileCount == 32, "Shard analysis file count mismatch.");

                Console.WriteLine("PASS: snapshot capture");
                Console.WriteLine("PASS: snapshot write");
                Console.WriteLine("PASS: snapshot read");
                Console.WriteLine("PASS: shard analysis");

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
