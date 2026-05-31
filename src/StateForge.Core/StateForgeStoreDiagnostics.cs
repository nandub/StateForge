namespace StateForge.Core
{
    public sealed class StateForgeStoreDiagnostics
    {
        public string RootPath { get; set; }
        public string SessionsPath { get; set; }
        public string TempPath { get; set; }
        public string BackupPath { get; set; }
        public string QuarantinePath { get; set; }
        public int SessionFileCount { get; set; }
        public int TempFileCount { get; set; }
        public int BackupFileCount { get; set; }
        public int QuarantineFileCount { get; set; }
    }
}
