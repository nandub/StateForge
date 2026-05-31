using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using StateForge.Core;

namespace StateForge.AspNetCore
{
    public sealed class StateForgeDistributedCache : IDistributedCache
    {
        private readonly IStateForgeStore _store;
        private readonly StateForgeDistributedCacheOptions _options;

        public StateForgeDistributedCache(IStateForgeStore store, StateForgeDistributedCacheOptions options)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _options = options ?? new StateForgeDistributedCacheOptions();
        }

        public byte[] Get(string key)
        {
            StateForgeEntry entry = _store.Get(key);
            return entry == null ? null : entry.Value;
        }

        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return Task.FromResult(Get(key));
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _store.Set(key, value, ResolveTimeout(options));
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        public void Refresh(string key)
        {
            _store.Refresh(key, TimeSpan.FromMinutes(_options.DefaultExpirationMinutes));
        }

        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            Refresh(key);
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _store.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            Remove(key);
            return Task.CompletedTask;
        }

        private TimeSpan ResolveTimeout(DistributedCacheEntryOptions options)
        {
            if (options == null) { return TimeSpan.FromMinutes(_options.DefaultExpirationMinutes); }
            if (options.SlidingExpiration.HasValue) { return options.SlidingExpiration.Value; }
            if (options.AbsoluteExpirationRelativeToNow.HasValue) { return options.AbsoluteExpirationRelativeToNow.Value; }
            if (options.AbsoluteExpiration.HasValue)
            {
                TimeSpan remaining = options.AbsoluteExpiration.Value.Subtract(DateTimeOffset.UtcNow);
                if (remaining > TimeSpan.Zero) { return remaining; }
            }
            return TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
        }
    }
}
