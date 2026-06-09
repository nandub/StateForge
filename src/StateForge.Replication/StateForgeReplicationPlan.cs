using System.Collections.Generic;

namespace StateForge.Replication
{
    public sealed class StateForgeReplicationPlan
    {
        public string PrimaryRootPath { get; set; }

        public string PrimarySessionsPath { get; set; }

        public List<StateForgeReplicationTarget> Targets { get; private set; }

        public StateForgeReplicationPlan()
        {
            Targets = new List<StateForgeReplicationTarget>();
        }
    }
}
