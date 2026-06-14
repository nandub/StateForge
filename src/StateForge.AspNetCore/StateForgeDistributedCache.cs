using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using StateForge.Core;

namespace StateForge.AspNetCore
{
    /// <summary>
    /// Implements <see cref="IDistributedCache"/> over an <see cref="IStateForgeStore"/>.
    /// </summary>
    /// <remarks>
    /// Cache values carry a small StateForge envelope that preserves sliding and absolute
    /// expiration independently. Legacy unenveloped values remain readable.
    /// </remarks>
    public sealed class StateForgeDistributedCache : IDistributedCache
    {
        private const int EnvelopeMagic = 0x53464348;
        private const int EnvelopeVersion = 1;
        private readonly IStateForgeStore _store;
        private readonly StateForgeDistributedCacheOptions _options;

        /// <summary>Initializes a new file-backed distributed cache.</summary>
        /// <param name="store">The StateForge store used for cache persistence.</param>
        /// <param name="options">Cache and file-store options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="store"/> is <see langword="null"/>.</exception>
        public StateForgeDistributedCache(IStateForgeStore store, StateForgeDistributedCacheOptions options)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _options = options ?? new StateForgeDistributedCacheOptions();
        }

        /// <inheritdoc/>
        public byte[] Get(string key)
        {
            StateForgeEntry entry = _store.Get(key);
            if (entry == null) { return null; }

            CacheEnvelope envelope;
            return TryReadEnvelope(entry.Value, out envelope) ? envelope.Value : entry.Value;
        }

        /// <inheritdoc/>
        public Task<byte[]> GetAsync(string key, CancellationToken token = default(CancellationToken))
        {
            return Task.FromResult(Get(key));
        }

        /// <inheritdoc/>
        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            CacheEnvelope envelope = CreateEnvelope(value, options, now);

            if (envelope.AbsoluteExpirationUtc.HasValue &&
                envelope.AbsoluteExpirationUtc.Value <= now)
            {
                _store.Remove(key);
                return;
            }

            _store.Set(key, WriteEnvelope(envelope), ResolveTimeout(envelope, now));
        }

        /// <inheritdoc/>
        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default(CancellationToken))
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Refresh(string key)
        {
            StateForgeEntry entry = _store.Get(key);
            if (entry == null) { return; }

            CacheEnvelope envelope;
            if (!TryReadEnvelope(entry.Value, out envelope))
            {
                _store.Refresh(key, TimeSpan.FromMinutes(_options.DefaultExpirationMinutes));
                return;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;

            if (envelope.AbsoluteExpirationUtc.HasValue &&
                envelope.AbsoluteExpirationUtc.Value <= now)
            {
                _store.Remove(key);
                return;
            }

            _store.Refresh(key, ResolveTimeout(envelope, now));
        }

        /// <inheritdoc/>
        public Task RefreshAsync(string key, CancellationToken token = default(CancellationToken))
        {
            Refresh(key);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Remove(string key)
        {
            _store.Remove(key);
        }

        /// <inheritdoc/>
        public Task RemoveAsync(string key, CancellationToken token = default(CancellationToken))
        {
            Remove(key);
            return Task.CompletedTask;
        }

        private CacheEnvelope CreateEnvelope(byte[] value, DistributedCacheEntryOptions options, DateTimeOffset now)
        {
            CacheEnvelope envelope = new CacheEnvelope();
            envelope.Value = value ?? new byte[0];

            if (options == null)
            {
                envelope.AbsoluteExpirationUtc = now.AddMinutes(_options.DefaultExpirationMinutes);
                return envelope;
            }

            envelope.SlidingExpiration = options.SlidingExpiration;

            DateTimeOffset? absolute = options.AbsoluteExpiration;
            if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                DateTimeOffset relativeAbsolute = now.Add(options.AbsoluteExpirationRelativeToNow.Value);
                if (!absolute.HasValue || relativeAbsolute < absolute.Value)
                {
                    absolute = relativeAbsolute;
                }
            }

            if (!absolute.HasValue && !envelope.SlidingExpiration.HasValue)
            {
                absolute = now.AddMinutes(_options.DefaultExpirationMinutes);
            }

            envelope.AbsoluteExpirationUtc = absolute;
            return envelope;
        }

        private TimeSpan ResolveTimeout(CacheEnvelope envelope, DateTimeOffset now)
        {
            TimeSpan timeout;

            if (envelope.SlidingExpiration.HasValue)
            {
                timeout = envelope.SlidingExpiration.Value;
            }
            else if (envelope.AbsoluteExpirationUtc.HasValue)
            {
                timeout = envelope.AbsoluteExpirationUtc.Value.Subtract(now);
            }
            else
            {
                timeout = TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
            }

            if (envelope.SlidingExpiration.HasValue && envelope.AbsoluteExpirationUtc.HasValue)
            {
                TimeSpan remaining = envelope.AbsoluteExpirationUtc.Value.Subtract(now);
                if (remaining < timeout)
                {
                    timeout = remaining;
                }
            }

            return timeout > TimeSpan.Zero ? timeout : TimeSpan.FromTicks(1);
        }

        private static byte[] WriteEnvelope(CacheEnvelope envelope)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(EnvelopeMagic);
                writer.Write(EnvelopeVersion);
                writer.Write(envelope.SlidingExpiration.HasValue
                    ? envelope.SlidingExpiration.Value.Ticks
                    : -1L);
                writer.Write(envelope.AbsoluteExpirationUtc.HasValue
                    ? envelope.AbsoluteExpirationUtc.Value.ToUnixTimeMilliseconds()
                    : -1L);
                writer.Write(envelope.Value.Length);
                writer.Write(envelope.Value);
                writer.Flush();
                return stream.ToArray();
            }
        }

        private static bool TryReadEnvelope(byte[] data, out CacheEnvelope envelope)
        {
            envelope = null;
            if (data == null || data.Length < 28) { return false; }

            try
            {
                using (MemoryStream stream = new MemoryStream(data, false))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != EnvelopeMagic ||
                        reader.ReadInt32() != EnvelopeVersion)
                    {
                        return false;
                    }

                    long slidingTicks = reader.ReadInt64();
                    long absoluteMilliseconds = reader.ReadInt64();
                    int length = reader.ReadInt32();

                    if (length < 0 || length != stream.Length - stream.Position)
                    {
                        return false;
                    }

                    CacheEnvelope parsed = new CacheEnvelope();
                    parsed.SlidingExpiration = slidingTicks >= 0
                        ? TimeSpan.FromTicks(slidingTicks)
                        : (TimeSpan?)null;
                    parsed.AbsoluteExpirationUtc = absoluteMilliseconds >= 0
                        ? DateTimeOffset.FromUnixTimeMilliseconds(absoluteMilliseconds)
                        : (DateTimeOffset?)null;
                    parsed.Value = reader.ReadBytes(length);
                    envelope = parsed;
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private sealed class CacheEnvelope
        {
            public byte[] Value { get; set; }
            public TimeSpan? SlidingExpiration { get; set; }
            public DateTimeOffset? AbsoluteExpirationUtc { get; set; }
        }
    }
}
