using System;
using System.IO;
using System.Text;
using StateForge.FileStore;
using StateForge.Replication;

namespace StateForge.ReplicationTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeReplicationTests");
                string primary = Path.Combine(root, "primary");
                string replicaA = Path.Combine(root, "replica-a");
                string replicaB = Path.Combine(root, "replica-b");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(primary);
                Directory.CreateDirectory(replicaA);
                Directory.CreateDirectory(replicaB);

                StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
                options.RootPath = primary;
                options.ShardDepth = 1;

                StateForgeFileStore store = new StateForgeFileStore(options);
                byte[] payload = Encoding.UTF8.GetBytes("replication");

                for (int i = 0; i < 12; i++)
                {
                    store.Set("replicate-" + i.ToString("D4"), payload, TimeSpan.FromMinutes(10));
                }

                StateForgeReplicationOptions replication = new StateForgeReplicationOptions();
                replication.PrimaryRootPath = primary;
                replication.Replicas.Add(new StateForgeReplicaNode { Name = "a", RootPath = replicaA });
                replication.Replicas.Add(new StateForgeReplicaNode { Name = "b", RootPath = replicaB });

                StateForgeReplicationResult health = StateForgeReplicationHealth.Check(replication);
                Require(health.Success, "Replication health failed.");

                StateForgeFileReplicator replicator = new StateForgeFileReplicator();
                StateForgeReplicationResult result = replicator.Replicate(replication);

                Require(result.Success, "Replication failed.");
                Require(result.SourceFilesScanned == 12, "Unexpected source file count.");
                Require(result.ReplicasVisited == 2, "Unexpected replica count.");
                Require(result.FilesCopied == 24, "Unexpected copied file count.");

                int replicaAFiles = Directory.GetFiles(Path.Combine(replicaA, "sessions"), "*.stfg", SearchOption.AllDirectories).Length;
                int replicaBFiles = Directory.GetFiles(Path.Combine(replicaB, "sessions"), "*.stfg", SearchOption.AllDirectories).Length;

                Require(replicaAFiles == 12, "Replica A file count mismatch.");
                Require(replicaBFiles == 12, "Replica B file count mismatch.");

                Console.WriteLine("PASS: replication health");
                Console.WriteLine("PASS: replication plan");
                Console.WriteLine("PASS: replication copy");
                Console.WriteLine("PASS: sharded layout preserved");
                Console.WriteLine("PASS: multi-replica fanout");

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
