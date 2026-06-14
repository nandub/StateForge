using System;
using System.IO;
using System.Text;
using StateForge.FileStore;
using StateForge.Replication;
using StateForge.Snapshots;

namespace StateForge.MultiSiteTests
{
    internal static class Program
    {
        private static int Main()
        {
            string root = Path.Combine(Path.GetTempPath(), "StateForgeMultiSiteTests");
            try
            {
                Reset(root);
                DateTimeOffset now = new DateTimeOffset(2026, 6, 13, 18, 0, 0, TimeSpan.Zero);
                string primaryRoot = Path.Combine(root, "site-a-primary");
                string recoveryRoot = Path.Combine(root, "site-b-recovery");
                string repository = Path.Combine(root, "snapshots");
                string drillRoot = Path.Combine(root, "restore-drill");

                CreatePrimaryStore(primaryRoot);
                StateForgeSiteState source = CreateSite(
                    primaryRoot,
                    "site-a",
                    "us-central",
                    StateForgeSiteRole.Primary,
                    now);
                StateForgeSiteState target = CreateSite(
                    recoveryRoot,
                    "site-b",
                    "us-east",
                    StateForgeSiteRole.Recovery,
                    now);
                StateForgeSiteStateStore.Write(primaryRoot, source);
                StateForgeSiteStateStore.Write(recoveryRoot, target);

                StateForgeSiteState persisted = StateForgeSiteStateStore.Read(recoveryRoot);
                Require(persisted.SiteName == "site-b", "Recovery site identity was not persisted.");
                Require(persisted.Region == "us-east", "Recovery site region was not persisted.");

                StateForgeQuorumResult quorum = EligibleQuorum("replica-site-b");
                StateForgeCrossSiteResult eligible = StateForgeCrossSiteEvaluator.Evaluate(
                    source,
                    persisted,
                    new StateForgeCrossSitePolicy(),
                    quorum,
                    "replica-site-b",
                    now);
                Require(eligible.Eligible, "Healthy cross-site target was rejected.");

                StateForgeSiteState stale = CreateSite(
                    Path.Combine(root, "stale-site"),
                    "site-c",
                    "us-west",
                    StateForgeSiteRole.Recovery,
                    now.AddMinutes(-20));
                StateForgeCrossSiteResult staleResult = StateForgeCrossSiteEvaluator.Evaluate(
                    source,
                    stale,
                    new StateForgeCrossSitePolicy(),
                    quorum,
                    "replica-site-b",
                    now);
                Require(!staleResult.Eligible, "Stale recovery point was accepted.");

                StateForgeSiteState sameRegion = CreateSite(
                    Path.Combine(root, "same-region"),
                    "site-d",
                    "us-central",
                    StateForgeSiteRole.Recovery,
                    now);
                StateForgeCrossSiteResult sameRegionResult = StateForgeCrossSiteEvaluator.Evaluate(
                    source,
                    sameRegion,
                    new StateForgeCrossSitePolicy(),
                    quorum,
                    "replica-site-b",
                    now);
                Require(!sameRegionResult.Eligible, "Same-region recovery target was accepted.");

                ReplicateAcrossSites(root, primaryRoot, recoveryRoot);
                RunRestoreDrill(primaryRoot, repository, drillRoot);
                RunFencedSiteFailover(root, recoveryRoot, eligible, quorum, now);
                VerifyPolicyRootMismatch(root, recoveryRoot, eligible, quorum, now);
                VerifyCorruptSiteState(root);

                Console.WriteLine("PASS: site metadata persistence");
                Console.WriteLine("PASS: cross-site replication metadata");
                Console.WriteLine("PASS: region and recovery-point policy");
                Console.WriteLine("PASS: multi-site snapshot restore drill");
                Console.WriteLine("PASS: fenced cross-site failover");
                Console.WriteLine("PASS: cross-site policy root binding");
                Console.WriteLine("PASS: corrupt site state rejection");

                Directory.Delete(root, true);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static void ReplicateAcrossSites(string root, string primaryRoot, string recoveryRoot)
        {
            string manifestPath = Path.Combine(root, "cross-site-manifest.json");
            StateForgeReplicationOptions options = new StateForgeReplicationOptions();
            options.PrimaryRootPath = primaryRoot;
            options.ManifestPath = manifestPath;
            options.Replicas.Add(new StateForgeReplicaNode
            {
                Name = "replica-site-b",
                RootPath = recoveryRoot,
                SiteName = "site-b",
                Region = "us-east"
            });

            StateForgeReplicationResult result = new StateForgeFileReplicator().Replicate(options);
            Require(result.Success, "Cross-site replication failed.");
            Require(result.FilesCopied == 5, "Unexpected cross-site file count.");
            string manifest = File.ReadAllText(manifestPath);
            Require(manifest.Contains("\"siteName\": \"site-b\""), "Manifest omitted target site.");
            Require(manifest.Contains("\"region\": \"us-east\""), "Manifest omitted target region.");
        }

        private static void RunRestoreDrill(string primaryRoot, string repository, string drillRoot)
        {
            StateForgeSnapshotOptions options = new StateForgeSnapshotOptions();
            options.SourceRootPath = primaryRoot;
            options.SnapshotRepositoryPath = repository;
            options.SnapshotName = "multi-site-drill";
            options.OverwriteExisting = true;

            StateForgeSnapshotService service = new StateForgeSnapshotService();
            StateForgeSnapshotResult snapshot = service.Create(options);
            Require(snapshot.Success, "Restore drill snapshot failed.");
            StateForgeSnapshotResult restore = service.Restore(snapshot.SnapshotPath, drillRoot, true);
            Require(restore.Success, "Multi-site restore drill failed.");
            Require(
                Directory.GetFiles(Path.Combine(drillRoot, "sessions"), "*.stfg", SearchOption.AllDirectories).Length == 5,
                "Restore drill file count mismatch.");
        }

        private static void RunFencedSiteFailover(
            string root,
            string recoveryRoot,
            StateForgeCrossSiteResult crossSite,
            StateForgeQuorumResult quorum,
            DateTimeOffset now)
        {
            string destination = Path.Combine(root, "site-b-primary");
            StateForgeFailoverOptions options = CreateFailoverOptions(
                root,
                recoveryRoot,
                destination,
                crossSite,
                quorum,
                now);

            StateForgeFailoverResult result = new StateForgeFailoverService().EvaluateAndFailover(options);
            Require(result.Success, "Fenced cross-site failover failed.");
            Require(result.CrossSitePolicy != null && result.CrossSitePolicy.Eligible,
                "Failover result omitted cross-site policy.");
            Require(File.Exists(result.MarkerPath), "Cross-site failover marker missing.");
            string marker = File.ReadAllText(result.MarkerPath);
            Require(marker.Contains("\"sourceSiteName\": \"site-a\""), "Failover marker omitted source site.");
            Require(marker.Contains("\"targetSiteName\": \"site-b\""), "Failover marker omitted target site.");
        }

        private static void VerifyPolicyRootMismatch(
            string root,
            string recoveryRoot,
            StateForgeCrossSiteResult eligible,
            StateForgeQuorumResult quorum,
            DateTimeOffset now)
        {
            StateForgeCrossSiteResult mismatched = new StateForgeCrossSiteResult
            {
                Eligible = true,
                SourceSiteName = eligible.SourceSiteName,
                TargetSiteName = eligible.TargetSiteName,
                TargetRootPath = Path.Combine(root, "different-replica"),
                CandidateName = eligible.CandidateName
            };
            string destination = Path.Combine(root, "mismatched-failover");
            StateForgeFailoverOptions options = CreateFailoverOptions(
                root,
                recoveryRoot,
                destination,
                mismatched,
                quorum,
                now.AddMinutes(1));

            StateForgeFailoverResult result = new StateForgeFailoverService().EvaluateAndFailover(options);
            Require(!result.Success, "Mismatched cross-site policy root was accepted.");
            Require(string.IsNullOrEmpty(result.MarkerPath), "Rejected cross-site failover wrote a marker.");

            StateForgeCrossSiteResult wrongCandidate = new StateForgeCrossSiteResult
            {
                Eligible = true,
                SourceSiteName = eligible.SourceSiteName,
                TargetSiteName = eligible.TargetSiteName,
                TargetRootPath = recoveryRoot,
                CandidateName = "different-candidate"
            };
            StateForgeFailoverOptions candidateOptions = CreateFailoverOptions(
                root,
                recoveryRoot,
                Path.Combine(root, "mismatched-candidate"),
                wrongCandidate,
                quorum,
                now.AddMinutes(2));
            StateForgeFailoverResult candidateResult =
                new StateForgeFailoverService().EvaluateAndFailover(candidateOptions);
            Require(!candidateResult.Success, "Mismatched cross-site candidate was accepted.");
        }

        private static StateForgeFailoverOptions CreateFailoverOptions(
            string root,
            string recoveryRoot,
            string destination,
            StateForgeCrossSiteResult crossSite,
            StateForgeQuorumResult quorum,
            DateTimeOffset now)
        {
            StateForgeFailoverOptions options = new StateForgeFailoverOptions();
            options.PrimaryRootPath = Path.Combine(root, "unavailable-primary");
            options.NewPrimaryRootPath = destination;
            options.ReplicaRootPaths.Add(recoveryRoot);
            options.RequireCrossSitePolicy = true;
            options.CrossSitePolicy = crossSite;
            options.RequirePromotionFence = true;
            options.PromotionFence = new StateForgePromotionFenceOptions
            {
                LeaseRootPath = Path.Combine(root, "leases-" + Path.GetFileName(destination)),
                ClusterName = "multi-site-cluster",
                CandidateName = "replica-site-b",
                QuorumResult = quorum,
                LeaseDuration = TimeSpan.FromMinutes(1),
                EvaluationUtc = now
            };
            return options;
        }

        private static void VerifyCorruptSiteState(string root)
        {
            string corruptRoot = Path.Combine(root, "corrupt-site");
            Directory.CreateDirectory(corruptRoot);
            File.WriteAllText(StateForgeSiteStateStore.GetPath(corruptRoot), "{ bad");
            bool rejected = false;
            try
            {
                StateForgeSiteStateStore.Read(corruptRoot);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Require(rejected, "Corrupt site state was accepted.");
        }

        private static StateForgeSiteState CreateSite(
            string root,
            string name,
            string region,
            StateForgeSiteRole role,
            DateTimeOffset recoveryPoint)
        {
            return new StateForgeSiteState
            {
                SiteName = name,
                Region = region,
                Role = role,
                RootPath = root,
                LastHeartbeatUtc = recoveryPoint,
                LastRecoveryPointUtc = recoveryPoint
            };
        }

        private static StateForgeQuorumResult EligibleQuorum(string candidate)
        {
            return new StateForgeQuorumResult
            {
                CandidateName = candidate,
                HasQuorum = true,
                CandidateEligible = true
            };
        }

        private static void CreatePrimaryStore(string primaryRoot)
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = primaryRoot;
            options.ShardDepth = 1;
            StateForgeFileStore store = new StateForgeFileStore(options);
            byte[] payload = Encoding.UTF8.GetBytes("multi-site");
            for (int i = 0; i < 5; i++)
            {
                store.Set("site-" + i.ToString("D4"), payload, TimeSpan.FromMinutes(30));
            }
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
