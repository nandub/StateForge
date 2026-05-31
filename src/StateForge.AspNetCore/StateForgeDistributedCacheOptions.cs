using StateForge.FileStore;

namespace StateForge.AspNetCore
{
    public sealed class StateForgeDistributedCacheOptions : StateForgeFileStoreOptions
    {
        public int DefaultExpirationMinutes { get; set; }

        public StateForgeDistributedCacheOptions()
        {
            DefaultExpirationMinutes = 20;
        }
    }
}
