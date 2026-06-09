using System;
using System.IO;
using System.Text;
using StateForge.FileStore;
using StateForge.Replication;
using StateForge.Snapshots;

namespace StateForge.RecoveryFlowTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeRecoveryFlowTests");
                string primary = Path.Combine(root, "primary");
                string replica = Path.Combine(root, "replica");
                string repository = Path.Combine(root, "snapshots");
                string restored = Path.Combine(root, "restored");
                string promoted = Path.Combine(root, "promoted");
                string failover = Path.Combine(root, "failover");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(primary);
                Directory.CreateDirectory(replica);

                StateForgeFileStoreOptions storeOptions = new StateForgeFileStoreOptions();
                storeOptions.RootPath = primary;
                storeOptions.ShardDepth = 1;

                StateForgeFileStore store = new StateForgeFileStore(storeOptions);
                byte[] payload = Encoding.UTF8.GetBytes("recovery-flow");

                for (int i = 0; i < 8; i++)
                {
                    store.Set("recovery-" + i.ToString("D4"), payload, TimeSpan.FromMinutes(10));
                }

                StateForgeReplicationOptions replicationOptions = new StateForgeReplicationOptions();
                replicationOptions.PrimaryRootPath = primary;
                replicationOptions.ManifestPath = Path.Combine(root, "replication-manifest.json");
                replicationOptions.Replicas.Add(new StateForgeReplicaNode { Name = "replica", RootPath = replica });

                StateForgeFileReplicator replicator = new StateForgeFileReplicator();
                StateForgeReplicationResult replication = replicator.Replicate(replicationOptions);
                Require(replication.Success, "Replication failed.");
                Require(replication.FilesCopied == 8, "Replication copied count mismatch.");
                Require(File.Exists(replicationOptions.ManifestPath), "Replication manifest missing.");

                StateForgeSnapshotService snapshotService = new StateForgeSnapshotService();
                StateForgeSnapshotOptions snapshotOptions = new StateForgeSnapshotOptions();
                snapshotOptions.SourceRootPath = primary;
                snapshotOptions.SnapshotRepositoryPath = repository;
                snapshotOptions.SnapshotName = "recovery-snapshot";
                snapshotOptions.OverwriteExisting = true;

                StateForgeSnapshotResult snapshot = snapshotService.Create(snapshotOptions);
                Require(snapshot.Success, "Snapshot failed.");
                Require(snapshot.FilesCopied == 8, "Snapshot count mismatch.");
                Require(File.Exists(snapshot.ManifestPath), "Snapshot manifest missing.");

                StateForgeSnapshotResult restore = snapshotService.Restore(snapshot.SnapshotPath, restored, true);
                Require(restore.Success, "Restore failed.");
                Require(Directory.GetFiles(Path.Combine(restored, "sessions"), "*.stfg", SearchOption.AllDirectories).Length == 8, "Restore count mismatch.");

                StateForgeReplicaPromotionService promotionService = new StateForgeReplicaPromotionService();
                StateForgeReplicaPromotionOptions promotionOptions = new StateForgeReplicaPromotionOptions();
                promotionOptions.ReplicaRootPath = replica;
                promotionOptions.NewPrimaryRootPath = promoted;
                promotionOptions.OverwriteExisting = true;

                StateForgeReplicaPromotionResult promotion = promotionService.Promote(promotionOptions);
                Require(promotion.Success, "Promotion failed.");
                Require(File.Exists(promotion.PromotionMarkerPath), "Promotion marker missing.");

                StateForgeFailoverService failoverService = new StateForgeFailoverService();
                StateForgeFailoverOptions failoverOptions = new StateForgeFailoverOptions();
                failoverOptions.PrimaryRootPath = Path.Combine(root, "missing-primary");
                failoverOptions.NewPrimaryRootPath = failover;
                failoverOptions.ReplicaRootPaths.Add(replica);

                StateForgeFailoverResult failoverResult = failoverService.EvaluateAndFailover(failoverOptions);
                Require(failoverResult.Success, "Failover failed.");
                Require(File.Exists(failoverResult.MarkerPath), "Failover marker missing.");

                Console.WriteLine("PASS: recovery replication");
                Console.WriteLine("PASS: recovery snapshot");
                Console.WriteLine("PASS: recovery restore");
                Console.WriteLine("PASS: recovery promotion");
                Console.WriteLine("PASS: recovery failover");

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
