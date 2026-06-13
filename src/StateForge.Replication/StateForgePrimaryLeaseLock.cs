using System;
using System.IO;
using System.Threading;

namespace StateForge.Replication
{
    internal sealed class StateForgePrimaryLeaseLock : IDisposable
    {
        private readonly StateForgeReplicaStateMutex localMutex;
        private readonly FileStream sharedLock;

        private StateForgePrimaryLeaseLock(
            StateForgeReplicaStateMutex localMutex,
            FileStream sharedLock)
        {
            this.localMutex = localMutex;
            this.sharedLock = sharedLock;
        }

        public static StateForgePrimaryLeaseLock Acquire(string leasePath)
        {
            string lockPath = leasePath + ".lock";
            string directory = Path.GetDirectoryName(Path.GetFullPath(lockPath));
            Directory.CreateDirectory(directory);

            StateForgeReplicaStateMutex local = StateForgeReplicaStateMutex.Acquire(leasePath);
            try
            {
                DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(30);
                while (true)
                {
                    try
                    {
                        FileStream stream = new FileStream(
                            lockPath,
                            FileMode.OpenOrCreate,
                            FileAccess.ReadWrite,
                            FileShare.None);
                        return new StateForgePrimaryLeaseLock(local, stream);
                    }
                    catch (IOException)
                    {
                        if (DateTimeOffset.UtcNow >= deadline)
                        {
                            throw new TimeoutException("Timed out waiting to acquire the shared primary lease lock.");
                        }

                        Thread.Sleep(50);
                    }
                }
            }
            catch
            {
                local.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            sharedLock.Dispose();
            localMutex.Dispose();
        }
    }
}
