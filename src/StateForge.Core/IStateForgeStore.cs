using System;

namespace StateForge.Core
{
    public interface IStateForgeStore : IStateForgeAdminStore
    {
        StateForgeEntry Get(string key);

        StateForgeLockResult GetAndLock(string key, TimeSpan lockTimeout);

        void Set(string key, byte[] value, TimeSpan timeout);

        bool SetAndUnlock(string key, byte[] value, TimeSpan timeout, long lockId);

        bool Unlock(string key, long lockId);

        bool Remove(string key);

        bool Refresh(string key, TimeSpan timeout);
    }
}
