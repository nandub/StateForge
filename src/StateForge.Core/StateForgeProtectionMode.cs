namespace StateForge.Core
{
    /// <summary>Specifies how a StateForge payload is protected at rest.</summary>
    public enum StateForgeProtectionMode
    {
        /// <summary>Do not encrypt the payload.</summary>
        None = 0,
        /// <summary>Use Windows Data Protection API protection.</summary>
        Dpapi = 1,
        /// <summary>Use Advanced Encryption Standard protection with a configured key.</summary>
        Aes = 2
    }
}
