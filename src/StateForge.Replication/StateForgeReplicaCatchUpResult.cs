using System.Collections.Generic;

namespace StateForge.Replication
{
    public sealed class StateForgeReplicaCatchUpResult
    {
        public bool Success { get; set; }

        public bool DryRun { get; set; }

        public int MissingFiles { get; set; }

        public int ChangedFiles { get; set; }

        public int ExtraFiles { get; set; }

        public int CopiedFiles { get; set; }

        public int DeletedFiles { get; set; }

        public int Errors { get; set; }

        public List<StateForgeReplicaCatchUpEntry> Entries { get; private set; }

        public StateForgeReplicaCatchUpResult()
        {
            Entries = new List<StateForgeReplicaCatchUpEntry>();
        }
    }
}
