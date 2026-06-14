namespace StateForge.Core
{
    /// <summary>Reports store directories and the number of files in each operational area.</summary>
    public sealed class StateForgeStoreDiagnostics
    {
        /// <summary>Gets or sets the configured store root.</summary>
        public string RootPath { get; set; }
        /// <summary>Gets or sets the session-record directory.</summary>
        public string SessionsPath { get; set; }
        /// <summary>Gets or sets the temporary-file directory.</summary>
        public string TempPath { get; set; }
        /// <summary>Gets or sets the backup-file directory.</summary>
        public string BackupPath { get; set; }
        /// <summary>Gets or sets the quarantine directory.</summary>
        public string QuarantinePath { get; set; }
        /// <summary>Gets or sets the number of session files.</summary>
        public int SessionFileCount { get; set; }
        /// <summary>Gets or sets the number of temporary files.</summary>
        public int TempFileCount { get; set; }
        /// <summary>Gets or sets the number of backup files.</summary>
        public int BackupFileCount { get; set; }
        /// <summary>Gets or sets the number of quarantined files.</summary>
        public int QuarantineFileCount { get; set; }
    }
}
