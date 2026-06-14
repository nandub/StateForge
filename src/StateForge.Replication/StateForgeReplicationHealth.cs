using System.IO;

namespace StateForge.Replication
{
    /// <summary>Provides state forge replication health operations.</summary>
    public static class StateForgeReplicationHealth
    {
        /// <summary>Performs the check operation.</summary>
        public static StateForgeReplicationResult Check(StateForgeReplicationOptions options)
        {
            StateForgeReplicationPlan plan = StateForgeReplicationPlanner.CreatePlan(options);
            StateForgeReplicationResult result = new StateForgeReplicationResult();

            if (!Directory.Exists(plan.PrimarySessionsPath))
            {
                result.Errors++;
                result.Messages.Add("Primary sessions path missing: " + plan.PrimarySessionsPath);
            }

            for (int i = 0; i < plan.Targets.Count; i++)
            {
                result.ReplicasVisited++;
                StateForgeReplicationTarget target = plan.Targets[i];

                if (!Directory.Exists(target.SessionsPath))
                {
                    try
                    {
                        Directory.CreateDirectory(target.SessionsPath);
                    }
                    catch
                    {
                        result.Errors++;
                        result.Messages.Add("Replica sessions path unavailable: " + target.SessionsPath);
                    }
                }
            }

            return result;
        }
    }
}
