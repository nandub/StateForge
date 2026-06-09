using System;
using System.IO;
using System.Text;
using StateForge.FileStore;
using StateForge.Replication;

namespace StateForge.ReplicationHostTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeReplicationHostTests");
                string primary = Path.Combine(root, "primary");
                string replica = Path.Combine(root, "replica");
                string manifest = Path.Combine(root, "manifest.json");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(primary);
                Directory.CreateDirectory(replica);

                StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
                options.RootPath = primary;
                options.ShardDepth = 1;

                StateForgeFileStore store = new StateForgeFileStore(options);
                byte[] payload = Encoding.UTF8.GetBytes("host");

                for (int i = 0; i < 4; i++)
                {
                    store.Set("host-" + i.ToString("D4"), payload, TimeSpan.FromMinutes(10));
                }

                StateForgeReplicationOptions replication = new StateForgeReplicationOptions();
                replication.PrimaryRootPath = primary;
                replication.ManifestPath = manifest;
                replication.Replicas.Add(new StateForgeReplicaNode { Name = "replica", RootPath = replica });

                StateForgeFileReplicator replicator = new StateForgeFileReplicator();
                StateForgeReplicationResult result = replicator.Replicate(replication);

                Require(result.Success, "Replication service failed.");
                Require(result.FilesCopied == 4, "Unexpected host copied file count.");
                Require(File.Exists(manifest), "Manifest was not written.");

                Console.WriteLine("PASS: replication service options");
                Console.WriteLine("PASS: replication service copy");
                Console.WriteLine("PASS: replication service manifest");

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
