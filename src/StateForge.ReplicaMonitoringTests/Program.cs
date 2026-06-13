using System;
using System.Collections.Generic;
using System.IO;
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

                string metrics = StateForgeReplicaPrometheusFormatter.Format(snapshot);
                Require(metrics.Contains("# TYPE stateforge_replica_lag_seconds gauge"), "Lag metric type missing.");
                Require(metrics.Contains("stateforge_replica_healthy{replica=\"healthy\""), "Healthy metric labels missing.");
                Require(metrics.Contains("stateforge_replica_lag_seconds{replica=\"stale\""), "Stale lag metric missing.");
                Require(metrics.Contains("stateforge_replica_catchup_operations_total{replica=\"healthy\""), "Catch-up metric missing.");
                Require(metrics.Contains("stateforge_replica_failed_syncs_total{replica=\"stale\""), "Failed sync metric missing.");

                Console.WriteLine("PASS: replica sync state persistence");
                Console.WriteLine("PASS: deterministic replica lag calculation");
                Console.WriteLine("PASS: stale replica detection");
                Console.WriteLine("PASS: missing replica state detection");
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

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
