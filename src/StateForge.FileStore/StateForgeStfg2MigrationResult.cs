namespace StateForge.FileStore
{
    public sealed class StateForgeStfg2MigrationResult
    {
        public string SourcePath { get; set; }

        public string DestinationPath { get; set; }

        public bool SourceWasStfg2 { get; set; }

        public bool Migrated { get; set; }

        public string KeyId { get; set; }

        public long OriginalLength { get; set; }

        public long NewLength { get; set; }
    }
}
