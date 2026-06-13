using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace StateForge.Replication
{
    internal sealed class StateForgeReplicaStateMutex : IDisposable
    {
        private readonly Mutex mutex;
        private bool ownsMutex;

        private StateForgeReplicaStateMutex(string statePath)
        {
            mutex = new Mutex(false, BuildName(statePath));
        }

        public static StateForgeReplicaStateMutex Acquire(string statePath)
        {
            StateForgeReplicaStateMutex handle = new StateForgeReplicaStateMutex(statePath);
            try
            {
                try
                {
                    handle.ownsMutex = handle.mutex.WaitOne(TimeSpan.FromSeconds(30));
                }
                catch (AbandonedMutexException)
                {
                    handle.ownsMutex = true;
                }

                if (!handle.ownsMutex)
                {
                    throw new TimeoutException("Timed out waiting to update replica sync state.");
                }

                return handle;
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            if (ownsMutex)
            {
                mutex.ReleaseMutex();
                ownsMutex = false;
            }

            mutex.Dispose();
        }

        private static string BuildName(string statePath)
        {
            byte[] input = Encoding.UTF8.GetBytes(statePath.ToUpperInvariant());
            using (SHA256 algorithm = SHA256.Create())
            {
                byte[] hash = algorithm.ComputeHash(input);
                StringBuilder builder = new StringBuilder("Local\\StateForge_ReplicaState_");
                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }
}
