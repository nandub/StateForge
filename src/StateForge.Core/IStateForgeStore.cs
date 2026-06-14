using System;

namespace StateForge.Core
{
    /// <summary>
    /// Defines keyed state operations together with the administrative operations exposed by a StateForge store.
    /// </summary>
    public interface IStateForgeStore : IStateForgeAdminStore
    {
        /// <summary>Gets an unexpired entry without acquiring a lock.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <returns>The entry, or <see langword="null"/> when it does not exist or has expired.</returns>
        StateForgeEntry Get(string key);

        /// <summary>Gets an entry and attempts to acquire its exclusive application lock.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <param name="lockTimeout">The age after which an existing lock is considered stale.</param>
        /// <returns>A result describing whether the entry was found, acquired, or held by another request.</returns>
        StateForgeLockResult GetAndLock(string key, TimeSpan lockTimeout);

        /// <summary>Creates or replaces an entry and clears any existing lock.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <param name="value">The binary payload.</param>
        /// <param name="timeout">The lifetime of the entry.</param>
        void Set(string key, byte[] value, TimeSpan timeout);

        /// <summary>Replaces an entry and releases its lock when the supplied fencing token matches.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <param name="value">The binary payload.</param>
        /// <param name="timeout">The new lifetime of the entry.</param>
        /// <param name="lockId">The lock token returned by <see cref="GetAndLock"/>.</param>
        /// <returns><see langword="true"/> when the update and unlock succeeded; otherwise, <see langword="false"/>.</returns>
        bool SetAndUnlock(string key, byte[] value, TimeSpan timeout, long lockId);

        /// <summary>Releases an entry lock when the supplied fencing token matches.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <param name="lockId">The lock token returned by <see cref="GetAndLock"/>.</param>
        /// <returns><see langword="true"/> when the lock was released; otherwise, <see langword="false"/>.</returns>
        bool Unlock(string key, long lockId);

        /// <summary>Removes an entry.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <returns><see langword="true"/> when an entry was removed; otherwise, <see langword="false"/>.</returns>
        bool Remove(string key);

        /// <summary>Extends the expiration time of an existing unexpired entry.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <param name="timeout">The new lifetime measured from the refresh operation.</param>
        /// <returns><see langword="true"/> when the entry was refreshed; otherwise, <see langword="false"/>.</returns>
        bool Refresh(string key, TimeSpan timeout);
    }
}
