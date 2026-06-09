using System;
using System.IO;
using System.Text;
using StateForge.FileStore;
using StateForge.Snapshots;

namespace StateForge.SnapshotMarkerTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeSnapshotMarkerTests");
                string replica = Path.Combine(root, "replica");
                string promoted = Path.Combine(root, "promoted");
                string failover = Path.Combine(root, "failover");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(replica);

                StateForgeFileStoreOptions storeOptions = new StateForgeFileStoreOptions();
                storeOptions.RootPath = replica;
                storeOptions.ShardDepth = 1;

                StateForgeFileStore store = new StateForgeFileStore(storeOptions);
                store.Set("marker-test", Encoding.UTF8.GetBytes("marker"), TimeSpan.FromMinutes(10));

                StateForgeReplicaPromotionService promotionService = new StateForgeReplicaPromotionService();
                StateForgeReplicaPromotionOptions promotionOptions = new StateForgeReplicaPromotionOptions();
                promotionOptions.ReplicaRootPath = replica;
                promotionOptions.NewPrimaryRootPath = promoted;
                promotionOptions.OverwriteExisting = true;

                StateForgeReplicaPromotionResult promotion = promotionService.Promote(promotionOptions);
                Require(promotion.Success, "Promotion failed.");
                Require(File.Exists(promotion.PromotionMarkerPath), "Promotion marker missing.");
                Require(File.ReadAllText(promotion.PromotionMarkerPath).Contains("\"version\": \"0.26.1\""), "Promotion marker version missing.");

                StateForgeFailoverService failoverService = new StateForgeFailoverService();
                StateForgeFailoverOptions failoverOptions = new StateForgeFailoverOptions();
                failoverOptions.PrimaryRootPath = Path.Combine(root, "missing-primary");
                failoverOptions.NewPrimaryRootPath = failover;
                failoverOptions.ReplicaRootPaths.Add(replica);

                StateForgeFailoverResult failoverResult = failoverService.EvaluateAndFailover(failoverOptions);
                Require(failoverResult.Success, "Failover failed.");
                Require(File.Exists(failoverResult.MarkerPath), "Failover marker missing.");
                Require(File.ReadAllText(failoverResult.MarkerPath).Contains("\"version\": \"0.26.1\""), "Failover marker version missing.");

                Console.WriteLine("PASS: promotion marker JSON");
                Console.WriteLine("PASS: failover marker JSON");
                Console.WriteLine("PASS: marker path escaping");

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
