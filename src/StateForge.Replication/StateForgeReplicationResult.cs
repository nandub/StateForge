using System.Collections.Generic;

namespace StateForge.Replication
{
    /// <summary>Represents state forge replication result.</summary>
    public sealed class StateForgeReplicationResult
    {
        /// <summary>Gets or sets the source files scanned.</summary>
        public int SourceFilesScanned { get; set; }

        /// <summary>Gets or sets the replicas visited.</summary>
        public int ReplicasVisited { get; set; }

        /// <summary>Gets or sets the files copied.</summary>
        public int FilesCopied { get; set; }

        /// <summary>Gets or sets the files skipped.</summary>
        public int FilesSkipped { get; set; }

        /// <summary>Gets or sets the conflicts.</summary>
        public int Conflicts { get; set; }

        /// <summary>Gets or sets the dry run.</summary>
        public bool DryRun { get; set; }

        /// <summary>Gets or sets the manifest path.</summary>
        public string ManifestPath { get; set; }

        /// <summary>Gets or sets the errors.</summary>
        public int Errors { get; set; }

        /// <summary>Gets the messages.</summary>
        public List<string> Messages { get; private set; }

        /// <summary>Gets or sets the manifest.</summary>
        public StateForgeReplicationManifest Manifest { get; set; }

        /// <summary>Gets whether replication completed without errors.</summary>
        public bool Success
        {
            get { return Errors == 0; }
        }

        /// <summary>Initializes a new instance of the <see cref="StateForgeReplicationResult"/> class.</summary>
        public StateForgeReplicationResult()
        {
            Messages = new List<string>();
        }
    }
}
