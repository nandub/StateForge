using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using StateForge.Core;
using StateForge.Remote.Protocol;

namespace StateForge.Remote.Host
{
    /// <summary>gRPC facade over an injected StateForge store.</summary>
    public sealed class StateForgeStoreGrpcService : StateForgeStoreRpc.StateForgeStoreRpcBase
    {
        private readonly IStateForgeStore _store;
        private readonly ILogger<StateForgeStoreGrpcService> _logger;

        /// <summary>Initializes the gRPC service.</summary>
        /// <param name="store">The backing StateForge store.</param>
        /// <param name="logger">The service logger.</param>
        public StateForgeStoreGrpcService(
            IStateForgeStore store,
            ILogger<StateForgeStoreGrpcService> logger)
        {
            _store = store ?? throw new ArgumentNullException("store");
            _logger = logger ?? throw new ArgumentNullException("logger");
        }

        /// <inheritdoc />
        public override Task<GetResponse> Get(GetRequest request, ServerCallContext context)
        {
            ValidateKey(request.Key);
            StateForgeEntry entry = _store.Get(request.Key);
            return Task.FromResult(new GetResponse { Found = entry != null, Entry = entry == null ? new StateForgeEntryDto() : ToDto(entry) });
        }

        /// <inheritdoc />
        public override Task<GetAndLockResponse> GetAndLock(GetAndLockRequest request, ServerCallContext context)
        {
            ValidateKey(request.Key);
            StateForgeLockResult result = _store.GetAndLock(request.Key, FromMilliseconds(request.LockTimeoutMilliseconds));

            GetAndLockResponse response = new GetAndLockResponse
            {
                Found = result.Found,
                LockedByOtherRequest = result.LockedByOtherRequest,
                LockId = result.LockId,
                LockAgeMilliseconds = checked((long)result.LockAge.TotalMilliseconds)
            };

            if (result.Entry != null)
            {
                response.Entry = ToDto(result.Entry);
            }

            return Task.FromResult(response);
        }

        /// <inheritdoc />
        public override Task<SetResponse> Set(SetRequest request, ServerCallContext context)
        {
            ValidateKey(request.Key);
            _store.Set(request.Key, request.Value.ToByteArray(), FromMilliseconds(request.TimeoutMilliseconds));
            return Task.FromResult(new SetResponse());
        }

        /// <inheritdoc />
        public override Task<SetAndUnlockResponse> SetAndUnlock(SetAndUnlockRequest request, ServerCallContext context)
        {
            ValidateKey(request.Key);
            bool updated = _store.SetAndUnlock(
                request.Key,
                request.Value.ToByteArray(),
                FromMilliseconds(request.TimeoutMilliseconds),
                request.LockId);

            return Task.FromResult(new SetAndUnlockResponse { Updated = updated });
        }

        /// <inheritdoc />
        public override Task<UnlockResponse> Unlock(UnlockRequest request, ServerCallContext context)
        {
            ValidateKey(request.Key);
            return Task.FromResult(new UnlockResponse { Unlocked = _store.Unlock(request.Key, request.LockId) });
        }

        /// <inheritdoc />
        public override Task<RemoveResponse> Remove(RemoveRequest request, ServerCallContext context)
        {
            ValidateKey(request.Key);
            return Task.FromResult(new RemoveResponse { Removed = _store.Remove(request.Key) });
        }

        /// <inheritdoc />
        public override Task<RefreshResponse> Refresh(RefreshRequest request, ServerCallContext context)
        {
            ValidateKey(request.Key);
            return Task.FromResult(new RefreshResponse
            {
                Refreshed = _store.Refresh(request.Key, FromMilliseconds(request.TimeoutMilliseconds))
            });
        }

        /// <inheritdoc />
        public override Task<EnumerateResponse> Enumerate(EnumerateRequest request, ServerCallContext context)
        {
            EnumerateResponse response = new EnumerateResponse();
            foreach (StateForgeEntryInfo entry in _store.Enumerate())
            {
                response.Entries.Add(ToDto(entry));
            }

            return Task.FromResult(response);
        }

        /// <inheritdoc />
        public override Task<GetDiagnosticsResponse> GetDiagnostics(GetDiagnosticsRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetDiagnosticsResponse { Diagnostics = ToDto(_store.GetDiagnostics()) });
        }

        /// <inheritdoc />
        public override Task<CleanupExpiredResponse> CleanupExpired(CleanupExpiredRequest request, ServerCallContext context)
        {
            return Task.FromResult(new CleanupExpiredResponse
            {
                Cleanup = ToDto(_store.CleanupExpired(request.QuarantineInvalid))
            });
        }

        /// <inheritdoc />
        public override Task<ForceRemoveResponse> ForceRemove(ForceRemoveRequest request, ServerCallContext context)
        {
            ValidateKey(request.Key);
            return Task.FromResult(new ForceRemoveResponse { Removed = _store.ForceRemove(request.Key) });
        }

        /// <inheritdoc />
        public override Task<GetStatsResponse> GetStats(GetStatsRequest request, ServerCallContext context)
        {
            return Task.FromResult(new GetStatsResponse { Stats = ToDto(_store.GetStats()) });
        }

        /// <inheritdoc />
        public override Task<ValidateConfigurationResponse> ValidateConfiguration(ValidateConfigurationRequest request, ServerCallContext context)
        {
            return Task.FromResult(new ValidateConfigurationResponse { Validation = ToDto(_store.ValidateConfiguration()) });
        }

        /// <inheritdoc />
        public override Task<CheckHealthResponse> CheckHealth(CheckHealthRequest request, ServerCallContext context)
        {
            return Task.FromResult(new CheckHealthResponse { Health = ToDto(_store.CheckHealth()) });
        }

        private static StateForgeEntryDto ToDto(StateForgeEntry entry)
        {
            StateForgeEntryDto dto = new StateForgeEntryDto
            {
                Key = entry.Key ?? string.Empty,
                Value = ByteString.CopyFrom(entry.Value ?? new byte[0]),
                CreatedUnixMs = entry.CreatedUtc.ToUnixTimeMilliseconds(),
                UpdatedUnixMs = entry.UpdatedUtc.ToUnixTimeMilliseconds(),
                ExpiresUnixMs = entry.ExpiresUtc.ToUnixTimeMilliseconds(),
                Locked = entry.Locked,
                LockId = entry.LockId
            };

            if (entry.LockDateUtc.HasValue)
            {
                dto.HasLockDate = true;
                dto.LockDateUnixMs = entry.LockDateUtc.Value.ToUnixTimeMilliseconds();
            }

            return dto;
        }

        private static StateForgeEntryInfoDto ToDto(StateForgeEntryInfo entry)
        {
            return new StateForgeEntryInfoDto
            {
                Key = entry.Key ?? string.Empty,
                CreatedUnixMs = entry.CreatedUtc.ToUnixTimeMilliseconds(),
                UpdatedUnixMs = entry.UpdatedUtc.ToUnixTimeMilliseconds(),
                ExpiresUnixMs = entry.ExpiresUtc.ToUnixTimeMilliseconds(),
                Locked = entry.Locked,
                LockId = entry.LockId,
                PhysicalPath = entry.PhysicalPath ?? string.Empty,
                PayloadLength = entry.PayloadLength,
                Expired = entry.Expired,
                Compressed = entry.Compressed,
                Encrypted = entry.Encrypted,
                AesEncrypted = entry.AesEncrypted
            };
        }

        private static StateForgeDiagnosticsDto ToDto(StateForgeStoreDiagnostics diagnostics)
        {
            return new StateForgeDiagnosticsDto
            {
                RootPath = diagnostics.RootPath ?? string.Empty,
                SessionsPath = diagnostics.SessionsPath ?? string.Empty,
                TempPath = diagnostics.TempPath ?? string.Empty,
                BackupPath = diagnostics.BackupPath ?? string.Empty,
                QuarantinePath = diagnostics.QuarantinePath ?? string.Empty,
                SessionFileCount = diagnostics.SessionFileCount,
                TempFileCount = diagnostics.TempFileCount,
                BackupFileCount = diagnostics.BackupFileCount,
                QuarantineFileCount = diagnostics.QuarantineFileCount
            };
        }

        private static StateForgeCleanupDto ToDto(StateForgeCleanupResult cleanup)
        {
            return new StateForgeCleanupDto
            {
                ExpiredDeleted = cleanup.ExpiredDeleted,
                InvalidQuarantined = cleanup.InvalidQuarantined,
                InvalidDeleted = cleanup.InvalidDeleted,
                Failed = cleanup.Failed
            };
        }

        private static StateForgeStatsDto ToDto(StateForgeStoreStats stats)
        {
            return new StateForgeStatsDto
            {
                TotalSessions = stats.TotalSessions,
                ExpiredSessions = stats.ExpiredSessions,
                LockedSessions = stats.LockedSessions,
                CompressedSessions = stats.CompressedSessions,
                EncryptedSessions = stats.EncryptedSessions,
                AesEncryptedSessions = stats.AesEncryptedSessions,
                TotalPayloadBytes = stats.TotalPayloadBytes,
                AveragePayloadBytes = stats.AveragePayloadBytes
            };
        }

        private static StateForgeValidationDto ToDto(StateForgeValidationResult validation)
        {
            StateForgeValidationDto dto = new StateForgeValidationDto();
            dto.Errors.Add(validation.Errors);
            dto.Warnings.Add(validation.Warnings);
            return dto;
        }

        private static StateForgeHealthDto ToDto(StateForgeHealthResult health)
        {
            StateForgeHealthDto dto = new StateForgeHealthDto
            {
                CanRead = health.CanRead,
                CanWrite = health.CanWrite,
                CanLock = health.CanLock,
                CanEnumerate = health.CanEnumerate,
                CanCleanup = health.CanCleanup
            };
            dto.Errors.Add(health.Errors);
            return dto;
        }

        private static TimeSpan FromMilliseconds(long milliseconds)
        {
            if (milliseconds <= 0)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Timeout must be positive."));
            }

            return TimeSpan.FromMilliseconds(milliseconds);
        }

        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "StateForge key is required."));
            }
        }
    }
}
