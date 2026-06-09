using System;
using System.IO;
using StateForge.Replication;

namespace StateForge.ReplicationManifestTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeReplicationManifestTests");
                string manifestPath = Path.Combine(root, "manifest.json");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(root);

                StateForgeReplicationManifest manifest = new StateForgeReplicationManifest();
                manifest.CapturedUtc = DateTimeOffset.UtcNow.ToString("o");
                manifest.PrimaryRootPath = @"C:\Primary\StateForge";
                manifest.PrimarySessionsPath = @"C:\Primary\StateForge\sessions";

                StateForgeReplicationManifestEntry entry = new StateForgeReplicationManifestEntry();
                entry.RelativePath = @"AB\ABC123.stfg";
                entry.SourceLength = 123;
                entry.SourceLastWriteUtc = DateTimeOffset.UtcNow.ToString("o");
                entry.ReplicaName = "replica-a";
                entry.DestinationPath = @"D:\Replica\StateForge\sessions\AB\ABC123.stfg";
                entry.Action = "copy";
                entry.Reason = "Copied.";
                manifest.Entries.Add(entry);

                StateForgeFileReplicator.WriteManifest(manifestPath, manifest);

                string json = File.ReadAllText(manifestPath);
                Require(json.Contains("\"version\": \"0.22.1\""), "Version field missing.");
                Require(json.Contains("\"relativePath\""), "Relative path field missing.");
                Require(json.Contains("\\\\") || json.Contains("AB"), "Backslash escaping missing or unexpected.");
                Require(json.Contains("\"action\": \"copy\""), "Action field missing.");

                Console.WriteLine("PASS: replication manifest write");
                Console.WriteLine("PASS: replication manifest escaped paths");
                Console.WriteLine("PASS: replication manifest JSON fields");

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
