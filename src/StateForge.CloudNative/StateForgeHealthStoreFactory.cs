using StateForge.FileStore;

namespace StateForge.CloudNative
{
    internal static class StateForgeHealthStoreFactory
    {
        public static StateForgeFileStore CreateFromEnvironment()
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = "/data/stateforge";
            options.EnableCompression = true;
            options.EnableEncryption = false;
            options.KeepBackups = false;
            options.ShardDepth = 1;

            StateForgeEnvironmentOptions.Apply(options);

            return new StateForgeFileStore(options);
        }
    }
}
