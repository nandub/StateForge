using System.Collections.Generic;

namespace StateForge.Replication
{
    public sealed class StateForgeReplicationResult
    {
        public int SourceFilesScanned { get; set; }

        public int ReplicasVisited { get; set; }

        public int FilesCopied { get; set; }

        public int FilesSkipped { get; set; }

        public int Errors { get; set; }

        public List<string> Messages { get; private set; }

        public bool Success
        {
            get { return Errors == 0; }
        }

        public StateForgeReplicationResult()
        {
            Messages = new List<string>();
        }
    }
}
