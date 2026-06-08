using System.Collections.Generic;

namespace StateForge.FileStore
{
    public sealed class StateForgeStfg2StoreMigrationResult
    {
        public string RootPath { get; set; }

        public bool DryRun { get; set; }

        public bool Applied { get; set; }

        public int FilesScanned { get; set; }

        public int LegacyFilesFound { get; set; }

        public int Stfg2FilesSkipped { get; set; }

        public int MigratedFiles { get; set; }

        public int FailedFiles { get; set; }

        public List<string> Errors { get; private set; }

        public StateForgeStfg2StoreMigrationResult()
        {
            Errors = new List<string>();
        }
    }
}
