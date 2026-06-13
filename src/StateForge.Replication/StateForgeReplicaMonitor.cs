using System;
using System.Collections.Generic;
using System.IO;

namespace StateForge.Replication
{
    public static class StateForgeReplicaMonitor
    {
        public static StateForgeReplicaMonitorSnapshot Capture(
            IEnumerable<StateForgeReplicaNode> replicas,
            TimeSpan staleThreshold)
        {
            return Capture(replicas, staleThreshold, DateTimeOffset.UtcNow);
        }

        public static StateForgeReplicaMonitorSnapshot Capture(
            IEnumerable<StateForgeReplicaNode> replicas,
            TimeSpan staleThreshold,
            DateTimeOffset capturedUtc)
        {
            if (staleThreshold < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException("staleThreshold");
            }

            StateForgeReplicaMonitorSnapshot snapshot = new StateForgeReplicaMonitorSnapshot();
            snapshot.CapturedUtc = capturedUtc;
            snapshot.StaleThreshold = staleThreshold;

            if (replicas == null) { return snapshot; }

            foreach (StateForgeReplicaNode replica in replicas)
            {
                if (replica == null || !replica.Enabled || string.IsNullOrWhiteSpace(replica.RootPath))
                {
                    continue;
                }

                StateForgeReplicaMonitorEntry entry = new StateForgeReplicaMonitorEntry();
                entry.ReplicaName = string.IsNullOrWhiteSpace(replica.Name) ? "replica" : replica.Name;
                entry.ReplicaRootPath = Path.GetFullPath(replica.RootPath);

                try
                {
                    StateForgeReplicaSyncState state = StateForgeReplicaStateStore.Read(entry.ReplicaRootPath);

                    if (state != null)
                    {
                        entry.LastAttemptUtc = state.LastAttemptUtc;
                        entry.LastSuccessfulSyncUtc = state.LastSuccessfulSyncUtc;
                        entry.CatchUpOperations = state.CatchUpOperations;
                        entry.FailedSyncs = state.FailedSyncs;
                        entry.LastError = state.LastError;
                    }

                    if (state == null || !state.LastSuccessfulSyncUtc.HasValue)
                    {
                        entry.LagSeconds = -1;
                        entry.Healthy = false;
                        entry.Stale = true;
                        entry.LastError = state == null ? "Replica sync state is missing." : state.LastError;
                    }
                    else
                    {
                        entry.LagSeconds = Math.Max(0, capturedUtc.Subtract(state.LastSuccessfulSyncUtc.Value).TotalSeconds);
                        entry.Stale = entry.LagSeconds > staleThreshold.TotalSeconds;
                        entry.Healthy = !entry.Stale &&
                            string.IsNullOrWhiteSpace(state.LastError);
                    }
                }
                catch (Exception ex)
                {
                    entry.LagSeconds = -1;
                    entry.Healthy = false;
                    entry.Stale = true;
                    entry.LastError = ex.GetType().Name + ": " + ex.Message;
                }

                snapshot.Replicas.Add(entry);
            }

            return snapshot;
        }
    }
}
