using StateForge.Core;

namespace StateForge.FileStore
{
    public class StateForgeFileStoreOptions : StateForgeOptions
    {
        public bool KeepBackups { get; set; }
        public int MaxPayloadBytes { get; set; }
        public int MutexTimeoutMilliseconds { get; set; }
        public bool UseWindowsDpapi { get; set; }
        public StateForgeProtectionMode ProtectionMode { get; set; }
        public string AesKeyBase64 { get; set; }

        public StateForgeFileStoreOptions()
        {
            KeepBackups = true;
            MaxPayloadBytes = 104857600;
            MutexTimeoutMilliseconds = 30000;
            UseWindowsDpapi = true;
            ProtectionMode = StateForgeProtectionMode.None;
        }
    }
}
