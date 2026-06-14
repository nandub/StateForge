namespace StateForge.FileStore
{
    /// <summary>Reports the result of migrating one record file to an STFG2 envelope.</summary>
    public sealed class StateForgeStfg2MigrationResult
    {
        /// <summary>Gets or sets the source file path.</summary>
        public string SourcePath { get; set; }

        /// <summary>Gets or sets the destination file path.</summary>
        public string DestinationPath { get; set; }

        /// <summary>Gets or sets a value indicating whether the source already used STFG2.</summary>
        public bool SourceWasStfg2 { get; set; }

        /// <summary>Gets or sets a value indicating whether legacy bytes were newly wrapped.</summary>
        public bool Migrated { get; set; }

        /// <summary>Gets or sets the key identifier requested for a new envelope.</summary>
        public string KeyId { get; set; }

        /// <summary>Gets or sets the source length in bytes.</summary>
        public long OriginalLength { get; set; }

        /// <summary>Gets or sets the destination length in bytes.</summary>
        public long NewLength { get; set; }
    }
}
