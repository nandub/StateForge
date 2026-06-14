using System;
using System.IO;
using System.Text;
using StateForge.Core;
using StateForge.FileStore;
using StateForge.Prometheus;

namespace StateForge.ApiValidationTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeApiValidationTests");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(root);

                StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
                options.RootPath = root;

                StateForgeFileStore store = new StateForgeFileStore(options);
                byte[] payload = Encoding.UTF8.GetBytes("api-validation");

                store.Set("api-key", payload, TimeSpan.FromMinutes(5));

                StateForgeEntry entry = store.Get("api-key");
                Require(entry != null, "StateForgeEntry was null.");

                byte[] entryBytes = entry.Value;
                Require(entryBytes != null, "StateForgeEntry byte payload was null.");
                Require(entryBytes.Length == payload.Length, "Payload length mismatch.");

                object stats = store.GetStats();
                Require(stats != null, "GetStats returned null.");

                string prometheus = StateForgePrometheusCollector.CollectText(root);
                Require(prometheus.IndexOf("stateforge_sessions_active", StringComparison.Ordinal) >= 0, "Prometheus active session metric missing.");

                Console.WriteLine("PASS: FileStore Set uses TimeSpan");
                Console.WriteLine("PASS: FileStore Get returns StateForgeEntry");
                Console.WriteLine("PASS: StateForgeEntry byte payload available");
                Console.WriteLine("PASS: FileStore GetStats available");
                Console.WriteLine("PASS: Prometheus collector compiles against FileStore");

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
