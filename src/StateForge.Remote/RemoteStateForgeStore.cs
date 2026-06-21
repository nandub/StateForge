using System;
using System.Collections.Generic;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Options;
using StateForge.Core;
using StateForge.Remote.Protocol;

namespace StateForge.Remote
{
    /// <summary>Remote implementation of <see cref="IStateForgeStore"/> backed by a gRPC/TLS StateForge service.</summary>
    /// <example>
    /// Use a StateForge remote endpoint alias to reach a TLS-backed gRPC store:
    /// <code>
    /// builder.Services.AddRemoteStateForgeStore(options =>
    /// {
    ///     options.Endpoint = "tcp:stateforge.internal:7443";
    ///     options.BearerToken = Environment.GetEnvironmentVariable("STATEFORGE_REMOTE_BEARER_TOKEN");
    ///     options.CallTimeout = TimeSpan.FromSeconds(5);
    /// });
    /// </code>
    /// </example>
    public sealed class RemoteStateForgeStore : IStateForgeStore
    {
        private readonly StateForgeStoreRpc.StateForgeStoreRpcClient _client;
        private readonly RemoteStateForgeOptions _options;

        /// <summary>Initializes a remote StateForge store.</summary>
        /// <param name="client">The generated gRPC client.</param>
        /// <param name="options">The configured remote options.</param>
        public RemoteStateForgeStore(
            StateForgeStoreRpc.StateForgeStoreRpcClient client,
            IOptions<RemoteStateForgeOptions> options)
        {
            _client = client ?? throw new ArgumentNullException("client");
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            _options = options.Value ?? throw new ArgumentNullException("options");
        }

        /// <inheritdoc />
        public StateForgeEntry Get(string key)
        {
            ValidateKey(key);
            GetResponse response = _client.Get(new GetRequest { Key = key }, deadline: CreateDeadline());
            return response.Found ? ToEntry(response.Entry) : null;
        }

        /// <inheritdoc />
        public StateForgeLockResult GetAndLock(string key, TimeSpan lockTimeout)
        {
            ValidateKey(key);
            ValidatePositive(lockTimeout, "lockTimeout");

            GetAndLockResponse response = _client.GetAndLock(
                new GetAndLockRequest
                {
                    Key = key,
                    LockTimeoutMilliseconds = ToMilliseconds(lockTimeout)
                },
                deadline: CreateDeadline());

            if (!response.Found)
            {
                return StateForgeLockResult.NotFound();
            }

            if (response.LockedByOtherRequest)
            {
                return StateForgeLockResult.Locked(
                    TimeSpan.FromMilliseconds(response.LockAgeMilliseconds),
                    response.LockId);
            }

            return StateForgeLockResult.Acquired(ToEntry(response.Entry));
        }

        /// <inheritdoc />
        public void Set(string key, byte[] value, TimeSpan timeout)
        {
            ValidateKey(key);
            ValidatePositive(timeout, "timeout");

            _client.Set(
                new SetRequest
                {
                    Key = key,
                    Value = ByteString.CopyFrom(value ?? new byte[0]),
                    TimeoutMilliseconds = ToMilliseconds(timeout)
                },
                deadline: CreateDeadline());
        }

        /// <inheritdoc />
        public bool SetAndUnlock(string key, byte[] value, TimeSpan timeout, long lockId)
        {
            ValidateKey(key);
            ValidatePositive(timeout, "timeout");

            SetAndUnlockResponse response = _client.SetAndUnlock(
                new SetAndUnlockRequest
                {
                    Key = key,
                    Value = ByteString.CopyFrom(value ?? new byte[0]),
                    TimeoutMilliseconds = ToMilliseconds(timeout),
                    LockId = lockId
                },
                deadline: CreateDeadline());

            return response.Updated;
        }

        /// <inheritdoc />
        public bool Unlock(string key, long lockId)
        {
            ValidateKey(key);
            UnlockResponse response = _client.Unlock(
                new UnlockRequest { Key = key, LockId = lockId },
                deadline: CreateDeadline());
            return response.Unlocked;
        }

        /// <inheritdoc />
        public bool Remove(string key)
        {
            ValidateKey(key);
            RemoveResponse response = _client.Remove(new RemoveRequest { Key = key }, deadline: CreateDeadline());
            return response.Removed;
        }

        /// <inheritdoc />
        public bool Refresh(string key, TimeSpan timeout)
        {
            ValidateKey(key);
            ValidatePositive(timeout, "timeout");

            RefreshResponse response = _client.Refresh(
                new RefreshRequest { Key = key, TimeoutMilliseconds = ToMilliseconds(timeout) },
                deadline: CreateDeadline());

            return response.Refreshed;
        }

        /// <inheritdoc />
        public IEnumerable<StateForgeEntryInfo> Enumerate()
        {
            EnumerateResponse response = _client.Enumerate(new EnumerateRequest(), deadline: CreateDeadline());
            foreach (StateForgeEntryInfoDto dto in response.Entries)
            {
                yield return ToEntryInfo(dto);
            }
        }

        /// <inheritdoc />
        public StateForgeStoreDiagnostics GetDiagnostics()
        {
            GetDiagnosticsResponse response = _client.GetDiagnostics(new GetDiagnosticsRequest(), deadline: CreateDeadline());
            return ToDiagnostics(response.Diagnostics);
        }

        /// <inheritdoc />
        public StateForgeCleanupResult CleanupExpired(bool quarantineInvalid)
        {
            CleanupExpiredResponse response = _client.CleanupExpired(
                new CleanupExpiredRequest { QuarantineInvalid = quarantineInvalid },
                deadline: CreateDeadline());
            return ToCleanup(response.Cleanup);
        }

        /// <inheritdoc />
        public bool ForceRemove(string key)
        {
            ValidateKey(key);
            ForceRemoveResponse response = _client.ForceRemove(
                new ForceRemoveRequest { Key = key },
                deadline: CreateDeadline());
            return response.Removed;
        }

        /// <inheritdoc />
        public StateForgeStoreStats GetStats()
        {
            GetStatsResponse response = _client.GetStats(new GetStatsRequest(), deadline: CreateDeadline());
            return ToStats(response.Stats);
        }

        /// <inheritdoc />
        public StateForgeValidationResult ValidateConfiguration()
        {
            StateForgeValidationResult result = new StateForgeValidationResult();

            try
            {
                StateForgeRemoteEndpoint.ToGrpcAddress(_options.Endpoint);
            }
            catch (ArgumentException ex)
            {
                result.AddError(ex.Message);
                return result;
            }

            ValidateConfigurationResponse response = _client.ValidateConfiguration(
                new ValidateConfigurationRequest(),
                deadline: CreateDeadline());

            foreach (string error in response.Validation.Errors)
            {
                result.AddError(error);
            }

            foreach (string warning in response.Validation.Warnings)
            {
                result.AddWarning(warning);
            }

            return result;
        }

        /// <inheritdoc />
        public StateForgeHealthResult CheckHealth()
        {
            try
            {
                CheckHealthResponse response = _client.CheckHealth(new CheckHealthRequest(), deadline: CreateDeadline());
                return ToHealth(response.Health);
            }
            catch (RpcException ex)
            {
                StateForgeHealthResult result = new StateForgeHealthResult();
                result.AddError(ex.StatusCode + ": remote StateForge health check failed.");
                return result;
            }
        }

        private DateTime CreateDeadline()
        {
            return DateTime.UtcNow.Add(_options.CallTimeout);
        }

        private static StateForgeEntry ToEntry(StateForgeEntryDto dto)
        {
            StateForgeEntry entry = new StateForgeEntry
            {
                Key = dto.Key,
                Value = dto.Value.ToByteArray(),
                CreatedUtc = DateTimeOffset.FromUnixTimeMilliseconds(dto.CreatedUnixMs),
                UpdatedUtc = DateTimeOffset.FromUnixTimeMilliseconds(dto.UpdatedUnixMs),
                ExpiresUtc = DateTimeOffset.FromUnixTimeMilliseconds(dto.ExpiresUnixMs),
                Locked = dto.Locked,
                LockId = dto.LockId
            };

            if (dto.HasLockDate)
            {
                entry.LockDateUtc = DateTimeOffset.FromUnixTimeMilliseconds(dto.LockDateUnixMs);
            }

            return entry;
        }

        private static StateForgeEntryInfo ToEntryInfo(StateForgeEntryInfoDto dto)
        {
            return new StateForgeEntryInfo
            {
                Key = dto.Key,
                CreatedUtc = DateTimeOffset.FromUnixTimeMilliseconds(dto.CreatedUnixMs),
                UpdatedUtc = DateTimeOffset.FromUnixTimeMilliseconds(dto.UpdatedUnixMs),
                ExpiresUtc = DateTimeOffset.FromUnixTimeMilliseconds(dto.ExpiresUnixMs),
                Locked = dto.Locked,
                LockId = dto.LockId,
                PhysicalPath = dto.PhysicalPath,
                PayloadLength = dto.PayloadLength,
                Expired = dto.Expired,
                Compressed = dto.Compressed,
                Encrypted = dto.Encrypted,
                AesEncrypted = dto.AesEncrypted
            };
        }

        private static StateForgeStoreDiagnostics ToDiagnostics(StateForgeDiagnosticsDto dto)
        {
            return new StateForgeStoreDiagnostics
            {
                RootPath = dto.RootPath,
                SessionsPath = dto.SessionsPath,
                TempPath = dto.TempPath,
                BackupPath = dto.BackupPath,
                QuarantinePath = dto.QuarantinePath,
                SessionFileCount = dto.SessionFileCount,
                TempFileCount = dto.TempFileCount,
                BackupFileCount = dto.BackupFileCount,
                QuarantineFileCount = dto.QuarantineFileCount
            };
        }

        private static StateForgeCleanupResult ToCleanup(StateForgeCleanupDto dto)
        {
            return new StateForgeCleanupResult
            {
                ExpiredDeleted = dto.ExpiredDeleted,
                InvalidQuarantined = dto.InvalidQuarantined,
                InvalidDeleted = dto.InvalidDeleted,
                Failed = dto.Failed
            };
        }

        private static StateForgeStoreStats ToStats(StateForgeStatsDto dto)
        {
            return new StateForgeStoreStats
            {
                TotalSessions = dto.TotalSessions,
                ExpiredSessions = dto.ExpiredSessions,
                LockedSessions = dto.LockedSessions,
                CompressedSessions = dto.CompressedSessions,
                EncryptedSessions = dto.EncryptedSessions,
                AesEncryptedSessions = dto.AesEncryptedSessions,
                TotalPayloadBytes = dto.TotalPayloadBytes,
                AveragePayloadBytes = dto.AveragePayloadBytes
            };
        }

        private static StateForgeHealthResult ToHealth(StateForgeHealthDto dto)
        {
            StateForgeHealthResult result = new StateForgeHealthResult
            {
                CanRead = dto.CanRead,
                CanWrite = dto.CanWrite,
                CanLock = dto.CanLock,
                CanEnumerate = dto.CanEnumerate,
                CanCleanup = dto.CanCleanup
            };

            foreach (string error in dto.Errors)
            {
                result.AddError(error);
            }

            return result;
        }

        private static long ToMilliseconds(TimeSpan value)
        {
            if (value.TotalMilliseconds > long.MaxValue)
            {
                throw new ArgumentOutOfRangeException("value", "Timeout is too large.");
            }

            return checked((long)value.TotalMilliseconds);
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("StateForge key is required.", "key");
            }
        }

        private static void ValidatePositive(TimeSpan value, string parameterName)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Timeout must be positive.");
            }
        }
    }
}
