using System;
using System.IO;
using System.Text;
using StateForge.FileStore;
using StateForge.Snapshots;

namespace StateForge.SnapshotServiceTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeSnapshotServiceTests");
                string primary = Path.Combine(root, "primary");
                string repository = Path.Combine(root, "snapshots");
                string restore = Path.Combine(root, "restore");
                string promoted = Path.Combine(root, "promoted");
                string failover = Path.Combine(root, "failover");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(primary);

                StateForgeFileStoreOptions storeOptions = new StateForgeFileStoreOptions();
                storeOptions.RootPath = primary;
                storeOptions.ShardDepth = 1;
                StateForgeFileStore store = new StateForgeFileStore(storeOptions);
                byte[] payload = Encoding.UTF8.GetBytes("snapshot-service");

                for (int i = 0; i < 6; i++)
                {
                    store.Set("snapshot-" + i.ToString("D4"), payload, TimeSpan.FromMinutes(10));
                }

                StateForgeSnapshotService snapshotService = new StateForgeSnapshotService();
                StateForgeSnapshotOptions snapshotOptions = new StateForgeSnapshotOptions();
                snapshotOptions.SourceRootPath = primary;
                snapshotOptions.SnapshotRepositoryPath = repository;
                snapshotOptions.SnapshotName = "snapshot-a";
                StateForgeSnapshotResult snapshotResult = snapshotService.Create(snapshotOptions);

                Require(snapshotResult.Success, "Snapshot create failed.");
                Require(snapshotResult.FilesCopied == 6, "Snapshot file count mismatch.");
                Require(File.Exists(snapshotResult.ManifestPath), "Snapshot manifest missing.");

                StateForgeSnapshotResult restoreResult = snapshotService.Restore(snapshotResult.SnapshotPath, restore, true);
                Require(restoreResult.Success, "Snapshot restore failed.");
                Require(Directory.GetFiles(Path.Combine(restore, "sessions"), "*.stfg", SearchOption.AllDirectories).Length == 6, "Restore file count mismatch.");

                StateForgeSnapshotScheduler scheduler = new StateForgeSnapshotScheduler();
                StateForgeSnapshotScheduleOptions schedule = new StateForgeSnapshotScheduleOptions();
                schedule.SourceRootPath = primary;
                schedule.SnapshotRepositoryPath = repository;
                schedule.RetainLast = 2;
                scheduler.RunOnce(schedule);
                scheduler.RunOnce(schedule);
                scheduler.RunOnce(schedule);
                Require(snapshotService.List(repository).Length == 2, "Snapshot retention failed.");

                StateForgeReplicaPromotionService promotion = new StateForgeReplicaPromotionService();
                StateForgeReplicaPromotionOptions promotionOptions = new StateForgeReplicaPromotionOptions();
                promotionOptions.ReplicaRootPath = snapshotResult.SnapshotPath;
                promotionOptions.NewPrimaryRootPath = promoted;
                promotionOptions.OverwriteExisting = true;
                StateForgeReplicaPromotionResult promotionResult = promotion.Promote(promotionOptions);
                Require(promotionResult.Success, "Promotion failed.");
                Require(File.Exists(promotionResult.PromotionMarkerPath), "Promotion marker missing.");

                StateForgeFailoverService failoverService = new StateForgeFailoverService();
                StateForgeFailoverOptions failoverOptions = new StateForgeFailoverOptions();
                failoverOptions.PrimaryRootPath = Path.Combine(root, "missing-primary");
                failoverOptions.NewPrimaryRootPath = failover;
                failoverOptions.ReplicaRootPaths.Add(snapshotResult.SnapshotPath);
                StateForgeFailoverResult failoverResult = failoverService.EvaluateAndFailover(failoverOptions);
                Require(failoverResult.Success, "Failover failed.");
                Require(File.Exists(failoverResult.MarkerPath), "Failover marker missing.");

                string failedPromotionRoot = Path.Combine(root, "failed-promotion");
                StateForgeReplicaPromotionOptions failedPromotionOptions = new StateForgeReplicaPromotionOptions();
                failedPromotionOptions.ReplicaRootPath = Path.Combine(root, "missing-replica");
                failedPromotionOptions.NewPrimaryRootPath = failedPromotionRoot;
                failedPromotionOptions.OverwriteExisting = true;
                StateForgeReplicaPromotionResult failedPromotion = promotion.Promote(failedPromotionOptions);
                Require(!failedPromotion.Success, "Missing replica promotion unexpectedly succeeded.");
                Require(string.IsNullOrEmpty(failedPromotion.PromotionMarkerPath), "Failed promotion returned a marker path.");
                Require(!File.Exists(Path.Combine(failedPromotionRoot, "promotion-marker.json")), "Failed promotion wrote a marker.");

                string corruptReplica = Path.Combine(root, "corrupt-replica");
                string corruptSessions = Path.Combine(corruptReplica, "sessions");
                Directory.CreateDirectory(corruptSessions);
                File.WriteAllText(Path.Combine(corruptSessions, "bad.stfg"), "bad");
                StateForgeFailoverOptions failedFailoverOptions = new StateForgeFailoverOptions();
                failedFailoverOptions.PrimaryRootPath = Path.Combine(root, "missing-primary-2");
                failedFailoverOptions.NewPrimaryRootPath = Path.Combine(root, "failed-failover");
                failedFailoverOptions.ReplicaRootPaths.Add(corruptReplica);
                StateForgeFailoverResult failedFailover = failoverService.EvaluateAndFailover(failedFailoverOptions);
                Require(!failedFailover.Success, "Corrupt replica failover unexpectedly succeeded.");
                Require(string.IsNullOrEmpty(failedFailover.MarkerPath), "Failed failover returned a marker path.");

                Console.WriteLine("PASS: snapshot create");
                Console.WriteLine("PASS: snapshot manifest");
                Console.WriteLine("PASS: snapshot restore");
                Console.WriteLine("PASS: snapshot scheduling retention");
                Console.WriteLine("PASS: replica promotion");
                Console.WriteLine("PASS: automatic failover");
                Console.WriteLine("PASS: failed recovery markers suppressed");

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
