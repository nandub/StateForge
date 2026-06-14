using StateForge.FileStore;

namespace StateForge.AspNetCore
{
    /// <summary>
    /// Configures the StateForge ASP.NET Core distributed cache and its underlying file store.
    /// </summary>
    public sealed class StateForgeDistributedCacheOptions : StateForgeFileStoreOptions
    {
        /// <summary>
        /// Gets or sets the default absolute expiration, in minutes, used when a cache entry
        /// does not specify an absolute or sliding expiration.
        /// </summary>
        public int DefaultExpirationMinutes { get; set; }

        /// <summary>Initializes options with a 20-minute default expiration.</summary>
        public StateForgeDistributedCacheOptions()
        {
            DefaultExpirationMinutes = 20;
        }
    }
}
