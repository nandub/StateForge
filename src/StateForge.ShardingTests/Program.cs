using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using StateForge.Core;
using StateForge.FileStore;
using StateForge.Performance;

namespace StateForge.ShardingTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeShardingTests");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(root);

                byte[] payload = Encoding.UTF8.GetBytes("sharded-session");

                StateForgeFileStoreOptions shardedOptions = new StateForgeFileStoreOptions();
                shardedOptions.RootPath = root;
                shardedOptions.ShardDepth = 1;

                StateForgeFileStore shardedStore = new StateForgeFileStore(shardedOptions);
                shardedStore.Set("sharded-key", payload, TimeSpan.FromMinutes(10));

                StateForgeEntry shardedEntry = shardedStore.Get("sharded-key");
                Require(shardedEntry != null, "Sharded entry missing.");
                Require(shardedEntry.Value.Length == payload.Length, "Sharded payload mismatch.");

                string hash = ComputeHash("sharded-key");
                string expectedShardFile = Path.Combine(root, "sessions", hash.Substring(0, 2), hash + ".stfg");
                Require(File.Exists(expectedShardFile), "Expected sharded file was not created.");

                StateForgeFileStoreOptions legacyOptions = new StateForgeFileStoreOptions();
                legacyOptions.RootPath = root;
                legacyOptions.ShardDepth = 0;

                StateForgeFileStore legacyWriter = new StateForgeFileStore(legacyOptions);
                legacyWriter.Set("legacy-key", payload, TimeSpan.FromMinutes(10));

                StateForgeEntry fallbackEntry = shardedStore.Get("legacy-key");
                Require(fallbackEntry != null, "Legacy fallback read failed.");
                Require(fallbackEntry.Value.Length == payload.Length, "Legacy fallback payload mismatch.");

                shardedStore.Remove("legacy-key");
                Require(legacyWriter.Get("legacy-key") == null, "Remove did not delete legacy-path entry.");

                StateForgeShardAnalysisResult analysis = StateForgeShardAnalyzer.Analyze(root);
                Require(analysis.FileCount >= 1, "Shard analysis did not see sharded files.");

                Console.WriteLine("PASS: sharded write");
                Console.WriteLine("PASS: sharded read");
                Console.WriteLine("PASS: legacy fallback read");
                Console.WriteLine("PASS: multi-depth remove");
                Console.WriteLine("PASS: shard analysis compatibility");

                Directory.Delete(root, true);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }


        private static string ComputeHash(string key)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] input = Encoding.UTF8.GetBytes(key);
                byte[] hash = sha256.ComputeHash(input);
                StringBuilder builder = new StringBuilder(hash.Length * 2);

                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("X2"));
                }

                return builder.ToString();
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
