using System;
using System.IO;
using System.Text;
using StateForge.Core;
using StateForge.FileStore;

namespace StateForge.ShardingMigrationHarness
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeShardingMigrationHarness");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(root);

                byte[] payload = Encoding.UTF8.GetBytes("migration");

                StateForgeFileStoreOptions legacyOptions = new StateForgeFileStoreOptions();
                legacyOptions.RootPath = root;
                legacyOptions.ShardDepth = 0;

                StateForgeFileStore legacyStore = new StateForgeFileStore(legacyOptions);

                for (int i = 0; i < 10; i++)
                {
                    legacyStore.Set("migrate-" + i.ToString("D4"), payload, TimeSpan.FromMinutes(10));
                }

                StateForgeFileStoreOptions shardedOptions = new StateForgeFileStoreOptions();
                shardedOptions.RootPath = root;
                shardedOptions.ShardDepth = 1;

                StateForgeFileStore shardedStore = new StateForgeFileStore(shardedOptions);

                for (int i = 0; i < 10; i++)
                {
                    string key = "migrate-" + i.ToString("D4");
                    StateForgeEntry entry = shardedStore.Get(key);
                    Require(entry != null, "Legacy fallback failed before migration.");

                    shardedStore.Remove(key);
                    shardedStore.Set(key, entry.Value, TimeSpan.FromMinutes(10));
                }

                int allFiles = Directory.GetFiles(Path.Combine(root, "sessions"), "*.stfg", SearchOption.AllDirectories).Length;
                int legacyRootFiles = Directory.GetFiles(Path.Combine(root, "sessions"), "*.stfg", SearchOption.TopDirectoryOnly).Length;

                Require(allFiles == 10, "Unexpected file count after migration.");
                Require(legacyRootFiles == 0, "Legacy root files remain after migration.");

                Console.WriteLine("PASS: sharding migration fallback read");
                Console.WriteLine("PASS: sharding migration rewrite");
                Console.WriteLine("PASS: sharding migration legacy cleanup");

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
