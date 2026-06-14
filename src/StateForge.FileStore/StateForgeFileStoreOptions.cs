using StateForge.Core;

namespace StateForge.FileStore
{
    /// <summary>Configures file persistence, limits, synchronization, compression, and encryption.</summary>
    public class StateForgeFileStoreOptions : StateForgeOptions
    {
        /// <summary>Gets or sets a value indicating whether replaced records are retained as backups.</summary>
        public bool KeepBackups { get; set; }
        /// <summary>Gets or sets the maximum decoded payload size in bytes.</summary>
        public int MaxPayloadBytes { get; set; }
        /// <summary>Gets or sets the named-mutex acquisition timeout in milliseconds.</summary>
        public int MutexTimeoutMilliseconds { get; set; }
        /// <summary>Gets or sets the legacy switch that selects Windows DPAPI when encryption is enabled.</summary>
        public bool UseWindowsDpapi { get; set; }
        /// <summary>Gets or sets the explicit payload-protection mode.</summary>
        public StateForgeProtectionMode ProtectionMode { get; set; }
        /// <summary>Gets or sets the Base64-encoded 128-bit, 192-bit, or 256-bit AES key.</summary>
        public string AesKeyBase64 { get; set; }

        /// <summary>Initializes options with backups and DPAPI compatibility enabled and a 100 MiB payload limit.</summary>
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
