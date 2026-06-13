using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using StateForge.Prometheus;
using StateForge.Replication;

namespace StateForge.ReplicaMonitoringTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeReplicaMonitoringTests");
                string healthyRoot = Path.Combine(root, "healthy");
                string staleRoot = Path.Combine(root, "stale");
                string missingRoot = Path.Combine(root, "missing");
                string corruptRoot = Path.Combine(root, "corrupt");
                string concurrentRoot = Path.Combine(root, "concurrent");
                DateTimeOffset now = new DateTimeOffset(2026, 6, 13, 12, 0, 0, TimeSpan.Zero);

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                StateForgeReplicaStateStore.RecordReplication(
                    healthyRoot, "healthy", true, string.Empty, now.AddSeconds(-30));
                StateForgeReplicaStateStore.RecordCatchUp(
                    healthyRoot, "healthy", true, string.Empty, now.AddSeconds(-20));
                StateForgeReplicaStateStore.RecordReplication(
                    staleRoot, "stale", true, string.Empty, now.AddMinutes(-20));
                StateForgeReplicaStateStore.RecordReplication(
                    staleRoot, "stale", false, "copy failed", now.AddMinutes(-10));

                List<StateForgeReplicaNode> replicas = new List<StateForgeReplicaNode>();
                replicas.Add(new StateForgeReplicaNode { Name = "healthy", RootPath = healthyRoot });
                replicas.Add(new StateForgeReplicaNode { Name = "stale", RootPath = staleRoot });
                replicas.Add(new StateForgeReplicaNode { Name = "missing", RootPath = missingRoot });

                StateForgeReplicaMonitorSnapshot snapshot = StateForgeReplicaMonitor.Capture(
                    replicas,
                    TimeSpan.FromMinutes(5),
                    now);

                Require(snapshot.Replicas.Count == 3, "Replica count mismatch.");
                Require(snapshot.Replicas[0].Healthy, "Fresh replica should be healthy.");
                Require(!snapshot.Replicas[0].Stale, "Fresh replica should not be stale.");
                Require(snapshot.Replicas[0].LagSeconds == 20, "Fresh replica lag mismatch.");
                Require(snapshot.Replicas[0].CatchUpOperations == 1, "Catch-up counter mismatch.");

                Require(!snapshot.Replicas[1].Healthy, "Failed stale replica should be unhealthy.");
                Require(snapshot.Replicas[1].Stale, "Old replica should be stale.");
                Require(snapshot.Replicas[1].LagSeconds == 1200, "Stale replica lag mismatch.");
                Require(snapshot.Replicas[1].FailedSyncs == 1, "Failed sync counter mismatch.");

                Require(!snapshot.Replicas[2].Healthy, "Missing state should be unhealthy.");
                Require(snapshot.Replicas[2].LagSeconds == -1, "Missing state lag sentinel mismatch.");

                VerifyStaleThresholdBoundary(root, now);
                VerifyCorruptState(corruptRoot, now);
                VerifyConcurrentUpdates(concurrentRoot, now);
                VerifyConfigurationParsing(root);

                string metrics = StateForgeReplicaPrometheusFormatter.Format(snapshot);
                Require(metrics.Contains("# TYPE stateforge_replica_lag_seconds gauge"), "Lag metric type missing.");
                Require(metrics.Contains("stateforge_replica_healthy{replica=\"healthy\""), "Healthy metric labels missing.");
                Require(metrics.Contains("stateforge_replica_lag_seconds{replica=\"stale\""), "Stale lag metric missing.");
                Require(metrics.Contains("stateforge_replica_catchup_operations_total{replica=\"healthy\""), "Catch-up metric missing.");
                Require(metrics.Contains("stateforge_replica_failed_syncs_total{replica=\"stale\""), "Failed sync metric missing.");

                StateForgeReplicaMonitorSnapshot escapedSnapshot = new StateForgeReplicaMonitorSnapshot();
                escapedSnapshot.Replicas.Add(new StateForgeReplicaMonitorEntry
                {
                    ReplicaName = "west\"one\\line\nnext",
                    ReplicaRootPath = "C:\\replicas\\west",
                    Healthy = true
                });
                string escapedMetrics = StateForgeReplicaPrometheusFormatter.Format(escapedSnapshot);
                Require(
                    escapedMetrics.Contains("replica=\"west\\\"one\\\\line\\nnext\""),
                    "Prometheus replica label escaping mismatch.");

                Console.WriteLine("PASS: replica sync state persistence");
                Console.WriteLine("PASS: deterministic replica lag calculation");
                Console.WriteLine("PASS: stale replica detection");
                Console.WriteLine("PASS: stale threshold boundary");
                Console.WriteLine("PASS: missing replica state detection");
                Console.WriteLine("PASS: corrupt replica state detection");
                Console.WriteLine("PASS: concurrent replica state updates");
                Console.WriteLine("PASS: named and positional replica configuration");
                Console.WriteLine("PASS: Prometheus label escaping");
                Console.WriteLine("PASS: multi-replica Prometheus metrics");

                Directory.Delete(root, true);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static void VerifyStaleThresholdBoundary(string root, DateTimeOffset now)
        {
            string exactRoot = Path.Combine(root, "threshold-exact");
            string overRoot = Path.Combine(root, "threshold-over");
            StateForgeReplicaStateStore.RecordReplication(
                exactRoot, "exact", true, string.Empty, now.AddMinutes(-5));
            StateForgeReplicaStateStore.RecordReplication(
                overRoot, "over", true, string.Empty, now.AddMinutes(-5).AddSeconds(-1));

            List<StateForgeReplicaNode> replicas = new List<StateForgeReplicaNode>();
            replicas.Add(new StateForgeReplicaNode { Name = "exact", RootPath = exactRoot });
            replicas.Add(new StateForgeReplicaNode { Name = "over", RootPath = overRoot });

            StateForgeReplicaMonitorSnapshot snapshot = StateForgeReplicaMonitor.Capture(
                replicas,
                TimeSpan.FromMinutes(5),
                now);
            Require(snapshot.Replicas[0].Healthy, "Replica at the stale threshold should remain healthy.");
            Require(!snapshot.Replicas[0].Stale, "Replica at the stale threshold should not be stale.");
            Require(snapshot.Replicas[1].Stale, "Replica over the stale threshold should be stale.");
        }

        private static void VerifyCorruptState(string root, DateTimeOffset now)
        {
            Directory.CreateDirectory(root);
            File.WriteAllText(
                StateForgeReplicaStateStore.GetPath(root),
                "{\"version\":\"1\",\"replicaName\":\"corrupt\"",
                new UTF8Encoding(false));

            List<StateForgeReplicaNode> replicas = new List<StateForgeReplicaNode>();
            replicas.Add(new StateForgeReplicaNode { Name = "corrupt", RootPath = root });
            StateForgeReplicaMonitorSnapshot snapshot = StateForgeReplicaMonitor.Capture(
                replicas,
                TimeSpan.FromMinutes(5),
                now);

            Require(!snapshot.Replicas[0].Healthy, "Corrupt state should be unhealthy.");
            Require(snapshot.Replicas[0].Stale, "Corrupt state should be stale.");
            Require(
                snapshot.Replicas[0].LastError.StartsWith("InvalidDataException:", StringComparison.Ordinal),
                "Corrupt state should report InvalidDataException.");
        }

        private static void VerifyConcurrentUpdates(string root, DateTimeOffset now)
        {
            const int operationCount = 24;
            Task[] tasks = new Task[operationCount];
            for (int i = 0; i < operationCount; i++)
            {
                int operation = i;
                tasks[i] = Task.Run(() =>
                    StateForgeReplicaStateStore.RecordCatchUp(
                        root,
                        "concurrent",
                        false,
                        "failure-" + operation,
                        now.AddSeconds(operation)));
            }

            Task.WaitAll(tasks);
            StateForgeReplicaSyncState state = StateForgeReplicaStateStore.Read(root);
            Require(state.CatchUpOperations == operationCount, "Concurrent catch-up updates were lost.");
            Require(state.FailedSyncs == operationCount, "Concurrent failure updates were lost.");
        }

        private static void VerifyConfigurationParsing(string root)
        {
            string firstRoot = Path.Combine(root, "named");
            string secondRoot = Path.Combine(root, "positional");
            List<StateForgeReplicaNode> replicas =
                StateForgeReplicaConfiguration.Parse("west=" + firstRoot + ";" + secondRoot);

            Require(replicas.Count == 2, "Replica configuration count mismatch.");
            Require(replicas[0].Name == "west", "Named replica configuration mismatch.");
            Require(replicas[0].RootPath == firstRoot, "Named replica path mismatch.");
            Require(replicas[1].Name == "replica-2", "Positional replica name mismatch.");
            Require(replicas[1].RootPath == secondRoot, "Positional replica path mismatch.");

            bool failed = false;
            try
            {
                StateForgeReplicaConfiguration.Parse("missing-path=");
            }
            catch (FormatException)
            {
                failed = true;
            }

            Require(failed, "Malformed replica configuration should fail.");
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
