using System.Collections.Generic;

namespace StateForge.Snapshots
{
    public sealed class StateForgeFailoverOptions
    {
        public string PrimaryRootPath { get; set; }

        public string NewPrimaryRootPath { get; set; }

        public List<string> ReplicaRootPaths { get; private set; }

        public bool Force { get; set; }

        public StateForgeFailoverOptions()
        {
            ReplicaRootPaths = new List<string>();
        }
    }
}
