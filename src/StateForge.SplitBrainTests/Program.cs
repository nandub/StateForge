using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using StateForge.Replication;
using StateForge.Snapshots;

namespace StateForge.SplitBrainTests
{
    internal static class Program
    {
        private static int Main()
        {
            string root = Path.Combine(Path.GetTempPath(), "StateForgeSplitBrainTests");
            try
            {
                Reset(root);
                DateTimeOffset now = new DateTimeOffset(2026, 6, 13, 12, 0, 0, TimeSpan.Zero);
                StateForgePromotionFenceService service = new StateForgePromotionFenceService();

                string leaseRoot = Path.Combine(root, "leases");
                StateForgePromotionFenceResult first = service.Acquire(
                    CreateOptions(leaseRoot, "replica-a", now, true));
                Require(first.Acquired, "Initial primary lease was not acquired.");
                Require(first.Lease.Epoch == 1, "Initial lease epoch must be one.");

                StateForgePromotionFenceResult blocked = service.Acquire(
                    CreateOptions(leaseRoot, "replica-b", now.AddSeconds(10), true));
                Require(!blocked.Acquired, "Active primary lease did not fence a rival candidate.");
                Require(blocked.Lease.PrimaryName == "replica-a", "Fencing result omitted the active primary.");

                StateForgePromotionFenceResult sameNameWithoutToken = service.Acquire(
                    CreateOptions(leaseRoot, "replica-a", now.AddSeconds(10), true));
                Require(!sameNameWithoutToken.Acquired, "Primary lease was reacquired without its ownership token.");

                StateForgePromotionFenceOptions ownerOptions =
                    CreateOptions(leaseRoot, "replica-a", now.AddSeconds(10), true);
                ownerOptions.LeaseId = first.Lease.LeaseId;
                StateForgePromotionFenceResult ownerReacquired = service.Acquire(ownerOptions);
                Require(ownerReacquired.Acquired, "Primary lease owner could not reacquire its lease.");

                StateForgePromotionFenceResult staleTakeover = service.Acquire(
                    CreateOptions(leaseRoot, "replica-b", now.AddSeconds(41), true));
                Require(staleTakeover.Acquired, "Stale primary lease did not allow takeover.");
                Require(staleTakeover.ExistingPrimaryStale, "Stale primary was not reported.");
                Require(staleTakeover.Lease.Epoch == 2, "Takeover did not advance the fencing epoch.");

                StateForgePromotionFenceResult noQuorum = service.Acquire(
                    CreateOptions(Path.Combine(root, "no-quorum"), "replica-a", now, false));
                Require(!noQuorum.Acquired, "Promotion without quorum was not rejected.");
                Require(!File.Exists(StateForgePrimaryLeaseStore.GetPath(Path.Combine(root, "no-quorum"))),
                    "Rejected promotion wrote a lease.");

                StateForgePromotionFenceResult wrongToken = service.Renew(
                    leaseRoot,
                    "cluster-a",
                    "replica-b",
                    "wrong-token",
                    TimeSpan.FromSeconds(30),
                    now.AddSeconds(35));
                Require(!wrongToken.Acquired, "Wrong ownership token renewed a lease.");

                StateForgePromotionFenceResult renewed = service.Renew(
                    leaseRoot,
                    "cluster-a",
                    "replica-b",
                    staleTakeover.Lease.LeaseId,
                    TimeSpan.FromSeconds(30),
                    now.AddSeconds(35));
                Require(renewed.Acquired, "Lease owner could not renew its lease.");
                Require(renewed.Lease.Epoch == 2, "Renewal changed the fencing epoch.");

                VerifyConcurrentAcquisition(root, now);
                VerifyFailoverSafety(root, now);
                VerifyCorruptLeaseRejected(root);

                Console.WriteLine("PASS: active primary promotion fencing");
                Console.WriteLine("PASS: stale-primary detection and epoch takeover");
                Console.WriteLine("PASS: quorum-required promotion fencing");
                Console.WriteLine("PASS: lease ownership token renewal");
                Console.WriteLine("PASS: concurrent promotion single winner");
                Console.WriteLine("PASS: failover safety marker suppression");
                Console.WriteLine("PASS: corrupt primary lease rejection");

                Directory.Delete(root, true);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static void VerifyConcurrentAcquisition(string root, DateTimeOffset now)
        {
            string leaseRoot = Path.Combine(root, "concurrent");
            StateForgePromotionFenceResult[] results = new StateForgePromotionFenceResult[2];
            Task first = Task.Run(delegate
            {
                results[0] = new StateForgePromotionFenceService().Acquire(
                    CreateOptions(leaseRoot, "replica-a", now, true));
            });
            Task second = Task.Run(delegate
            {
                results[1] = new StateForgePromotionFenceService().Acquire(
                    CreateOptions(leaseRoot, "replica-b", now, true));
            });
            Task.WaitAll(first, second);

            int acquired = (results[0].Acquired ? 1 : 0) + (results[1].Acquired ? 1 : 0);
            Require(acquired == 1, "Concurrent candidates must produce exactly one lease owner.");
        }

        private static void VerifyFailoverSafety(string root, DateTimeOffset now)
        {
            string leaseRoot = Path.Combine(root, "failover-leases");
            StateForgePromotionFenceResult primary = new StateForgePromotionFenceService().Acquire(
                CreateOptions(leaseRoot, "primary-a", now, true));
            Require(primary.Acquired, "Could not establish the active primary lease.");

            string replicaRoot = Path.Combine(root, "replica");
            Directory.CreateDirectory(Path.Combine(replicaRoot, "sessions"));
            string destination = Path.Combine(root, "blocked-failover");

            StateForgeFailoverOptions blockedOptions = new StateForgeFailoverOptions();
            blockedOptions.PrimaryRootPath = Path.Combine(root, "missing-primary");
            blockedOptions.NewPrimaryRootPath = destination;
            blockedOptions.ReplicaRootPaths.Add(replicaRoot);
            blockedOptions.RequirePromotionFence = true;
            blockedOptions.PromotionFence = CreateOptions(
                leaseRoot,
                "replica-b",
                now.AddSeconds(10),
                true);

            StateForgeFailoverResult blocked = new StateForgeFailoverService().EvaluateAndFailover(blockedOptions);
            Require(!blocked.Success, "Fenced failover unexpectedly succeeded.");
            Require(string.IsNullOrEmpty(blocked.MarkerPath), "Fenced failover wrote a marker.");

            blockedOptions.PromotionFence.EvaluationUtc = now.AddSeconds(31);
            StateForgeFailoverResult allowed = new StateForgeFailoverService().EvaluateAndFailover(blockedOptions);
            Require(allowed.Success, "Failover after stale-primary detection failed.");
            Require(File.Exists(allowed.MarkerPath), "Successful fenced failover did not write a marker.");

            StateForgeReplicaPromotionOptions missingFence = new StateForgeReplicaPromotionOptions();
            missingFence.ReplicaRootPath = replicaRoot;
            missingFence.NewPrimaryRootPath = Path.Combine(root, "missing-fence");
            missingFence.RequirePromotionFence = true;
            StateForgeReplicaPromotionResult rejected =
                new StateForgeReplicaPromotionService().Promote(missingFence);
            Require(!rejected.Success, "Required promotion fence was not enforced.");
        }

        private static void VerifyCorruptLeaseRejected(string root)
        {
            string leaseRoot = Path.Combine(root, "corrupt");
            Directory.CreateDirectory(leaseRoot);
            File.WriteAllText(StateForgePrimaryLeaseStore.GetPath(leaseRoot), "{ bad");
            bool rejected = false;
            try
            {
                StateForgePrimaryLeaseStore.Read(leaseRoot);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Require(rejected, "Corrupt primary lease was accepted.");
        }

        private static StateForgePromotionFenceOptions CreateOptions(
            string leaseRoot,
            string candidate,
            DateTimeOffset now,
            bool eligible)
        {
            StateForgePromotionFenceOptions options = new StateForgePromotionFenceOptions();
            options.LeaseRootPath = leaseRoot;
            options.ClusterName = "cluster-a";
            options.CandidateName = candidate;
            options.LeaseDuration = TimeSpan.FromSeconds(30);
            options.EvaluationUtc = now;
            options.QuorumResult = new StateForgeQuorumResult
            {
                CandidateName = candidate,
                HasQuorum = eligible,
                CandidateEligible = eligible
            };
            return options;
        }

        private static void Reset(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
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
