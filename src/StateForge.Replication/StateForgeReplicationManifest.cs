using System.Collections.Generic;

namespace StateForge.Replication
{
    public sealed class StateForgeReplicationManifest
    {
        public string CapturedUtc { get; set; }

        public string PrimaryRootPath { get; set; }

        public string PrimarySessionsPath { get; set; }

        public List<StateForgeReplicationManifestEntry> Entries { get; private set; }

        public StateForgeReplicationManifest()
        {
            Entries = new List<StateForgeReplicationManifestEntry>();
        }
    }
}
