using System;
using System.IO;

namespace StateForge.Replication
{
    /// <summary>Provides state forge replication planner operations.</summary>
    public static class StateForgeReplicationPlanner
    {
        /// <summary>Creates a validated replication plan from the supplied options.</summary>
        public static StateForgeReplicationPlan CreatePlan(StateForgeReplicationOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (string.IsNullOrWhiteSpace(options.PrimaryRootPath))
            {
                throw new ArgumentException("PrimaryRootPath is required.", "options");
            }

            StateForgeReplicationPlan plan = new StateForgeReplicationPlan();
            plan.PrimaryRootPath = Path.GetFullPath(options.PrimaryRootPath);

            string primarySessionsPath = ResolveSessionsPath(plan.PrimaryRootPath);
            plan.PrimarySessionsPath = primarySessionsPath;

            for (int i = 0; i < options.Replicas.Count; i++)
            {
                StateForgeReplicaNode replica = options.Replicas[i];

                if (replica == null || !replica.Enabled || string.IsNullOrWhiteSpace(replica.RootPath))
                {
                    continue;
                }

                StateForgeReplicationTarget target = new StateForgeReplicationTarget();
                target.Name = string.IsNullOrWhiteSpace(replica.Name) ? "replica-" + i.ToString() : replica.Name;
                target.RootPath = Path.GetFullPath(replica.RootPath);
                target.SessionsPath = ResolveSessionsPath(target.RootPath);
                target.SiteName = string.IsNullOrWhiteSpace(replica.SiteName)
                    ? string.Empty
                    : replica.SiteName.Trim();
                target.Region = string.IsNullOrWhiteSpace(replica.Region)
                    ? string.Empty
                    : replica.Region.Trim();

                plan.Targets.Add(target);
            }

            return plan;
        }

        internal static string ResolveSessionsPath(string rootPath)
        {
            string sessions = Path.Combine(rootPath, "sessions");
            return Directory.Exists(sessions) || !Path.HasExtension(rootPath) ? sessions : rootPath;
        }
    }
}
