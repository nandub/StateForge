namespace StateForge.Core
{
    /// <summary>Defines common filesystem, locking, sharding, compression, and encryption options.</summary>
    public class StateForgeOptions
    {
        /// <summary>Gets or sets the root directory used by the store.</summary>
        public string RootPath { get; set; }
        /// <summary>Gets or sets the age in minutes after which a lock is considered stale.</summary>
        public int StaleLockMinutes { get; set; }
        /// <summary>Gets or sets the number of directory-sharding levels.</summary>
        public int ShardDepth { get; set; }
        /// <summary>Gets or sets a value indicating whether payload compression is enabled.</summary>
        public bool EnableCompression { get; set; }
        /// <summary>Gets or sets a value indicating whether payload encryption is enabled.</summary>
        public bool EnableEncryption { get; set; }

        /// <summary>Initializes options with a five-minute stale-lock threshold and one sharding level.</summary>
        public StateForgeOptions()
        {
            StaleLockMinutes = 5;
            ShardDepth = 1;
        }
    }
}
