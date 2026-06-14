using System.Collections.Generic;

namespace StateForge.Replication
{
    /// <summary>Represents state forge replication manifest.</summary>
    public sealed class StateForgeReplicationManifest
    {
        /// <summary>Gets or sets the captured utc.</summary>
        public string CapturedUtc { get; set; }

        /// <summary>Gets or sets the primary root path.</summary>
        public string PrimaryRootPath { get; set; }

        /// <summary>Gets or sets the primary sessions path.</summary>
        public string PrimarySessionsPath { get; set; }

        /// <summary>Gets the entries.</summary>
        public List<StateForgeReplicationManifestEntry> Entries { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeReplicationManifest"/> class.</summary>
        public StateForgeReplicationManifest()
        {
            Entries = new List<StateForgeReplicationManifestEntry>();
        }
    }
}
