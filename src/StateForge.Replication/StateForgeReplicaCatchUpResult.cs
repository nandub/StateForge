using System.Collections.Generic;

namespace StateForge.Replication
{
    /// <summary>Represents state forge replica catch up result.</summary>
    public sealed class StateForgeReplicaCatchUpResult
    {
        /// <summary>Gets or sets the success.</summary>
        public bool Success { get; set; }

        /// <summary>Gets or sets the dry run.</summary>
        public bool DryRun { get; set; }

        /// <summary>Gets or sets the missing files.</summary>
        public int MissingFiles { get; set; }

        /// <summary>Gets or sets the changed files.</summary>
        public int ChangedFiles { get; set; }

        /// <summary>Gets or sets the extra files.</summary>
        public int ExtraFiles { get; set; }

        /// <summary>Gets or sets the copied files.</summary>
        public int CopiedFiles { get; set; }

        /// <summary>Gets or sets the deleted files.</summary>
        public int DeletedFiles { get; set; }

        /// <summary>Gets or sets the errors.</summary>
        public int Errors { get; set; }

        /// <summary>Gets the entries.</summary>
        public List<StateForgeReplicaCatchUpEntry> Entries { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="StateForgeReplicaCatchUpResult"/> class.</summary>
        public StateForgeReplicaCatchUpResult()
        {
            Entries = new List<StateForgeReplicaCatchUpEntry>();
        }
    }
}
