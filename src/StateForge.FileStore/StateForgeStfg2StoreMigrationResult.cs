using System.Collections.Generic;

namespace StateForge.FileStore
{
    /// <summary>Reports the outcome of scanning and optionally migrating a store tree to STFG2 envelopes.</summary>
    public sealed class StateForgeStfg2StoreMigrationResult
    {
        /// <summary>Gets or sets the scanned root path.</summary>
        public string RootPath { get; set; }

        /// <summary>Gets or sets a value indicating whether the scan was requested as a dry run.</summary>
        public bool DryRun { get; set; }

        /// <summary>Gets or sets a value indicating whether write operations were enabled.</summary>
        public bool Applied { get; set; }

        /// <summary>Gets or sets the number of matching files scanned.</summary>
        public int FilesScanned { get; set; }

        /// <summary>Gets or sets the number of legacy files found.</summary>
        public int LegacyFilesFound { get; set; }

        /// <summary>Gets or sets the number of existing STFG2 files skipped.</summary>
        public int Stfg2FilesSkipped { get; set; }

        /// <summary>Gets or sets the number of legacy files successfully migrated.</summary>
        public int MigratedFiles { get; set; }

        /// <summary>Gets or sets the number of files that failed inspection or migration.</summary>
        public int FailedFiles { get; set; }

        /// <summary>Gets the mutable list of per-file error descriptions.</summary>
        public List<string> Errors { get; private set; }

        /// <summary>Initializes an empty store-migration result.</summary>
        public StateForgeStfg2StoreMigrationResult()
        {
            Errors = new List<string>();
        }
    }
}
