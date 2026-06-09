using System;
using StateForge.Core;
using StateForge.FileStore;

namespace StateForge.Maintenance.Host
{
    internal static class StateForgeMaintenanceHostRunner
    {
        public static StateForgeMaintenanceHostResult RunOnce(StateForgeMaintenanceHostConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (string.IsNullOrWhiteSpace(config.RootPath))
            {
                throw new InvalidOperationException("RootPath is required.");
            }

            StateForgeMaintenanceHostResult result = new StateForgeMaintenanceHostResult();
            result.RootPath = config.RootPath;
            result.StartedUtc = DateTimeOffset.UtcNow.ToString("o");

            try
            {
                StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
                options.RootPath = config.RootPath;
                StateForgeFileStore store = new StateForgeFileStore(options);

                if (config.CleanupEnabled)
                {
                    StateForgeCleanupResult cleanup = store.CleanupExpired(true);
                    result.CleanupRan = true;
                    result.CleanupExpiredDeleted = cleanup.ExpiredDeleted;
                    result.CleanupInvalidQuarantined = cleanup.InvalidQuarantined;
                    result.CleanupFailed = cleanup.Failed;
                }

                if (config.HealthEnabled)
                {
                    StateForgeHealthResult health = store.CheckHealth();
                    result.HealthRan = true;
                    result.HealthEvaluated = true;
                    result.Healthy = health.Healthy;
                    result.CanRead = health.CanRead;
                    result.CanWrite = health.CanWrite;
                    result.CanLock = health.CanLock;
                    result.CanEnumerate = health.CanEnumerate;
                    result.CanCleanup = health.CanCleanup;

                    for (int i = 0; i < health.Errors.Count; i++)
                    {
                        result.Errors.Add(health.Errors[i]);
                    }
                }

                if (config.StatsEnabled)
                {
                    StateForgeStoreStats stats = store.GetStats();
                    result.StatsRan = true;
                    result.TotalSessions = stats.TotalSessions;
                    result.ExpiredSessions = stats.ExpiredSessions;
                    result.LockedSessions = stats.LockedSessions;
                    result.CompressedSessions = stats.CompressedSessions;
                    result.EncryptedSessions = stats.EncryptedSessions;
                    result.AesEncryptedSessions = stats.AesEncryptedSessions;
                    result.TotalPayloadBytes = stats.TotalPayloadBytes;
                    result.AveragePayloadBytes = stats.AveragePayloadBytes;
                }

                if (config.Stfg2MigrationEnabled)
                {
                    StateForgeStfg2StoreMigrationResult migration = StateForgeStfg2StoreMigrator.MigrateStore(
                        config.RootPath,
                        config.Stfg2MigrationKeyId,
                        config.Stfg2MigrationDryRun,
                        config.Stfg2MigrationApply,
                        config.Stfg2MigrationPattern);

                    result.MigrationRan = true;
                    result.MigrationFilesScanned = migration.FilesScanned;
                    result.MigrationLegacyFilesFound = migration.LegacyFilesFound;
                    result.MigrationStfg2FilesSkipped = migration.Stfg2FilesSkipped;
                    result.MigrationMigratedFiles = migration.MigratedFiles;
                    result.MigrationFailedFiles = migration.FailedFiles;

                    for (int i = 0; i < migration.Errors.Count; i++)
                    {
                        result.Errors.Add(migration.Errors[i]);
                    }
                }

                result.Success = result.Errors.Count == 0 && result.CleanupFailed == 0 && result.MigrationFailedFiles == 0;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add(ex.GetType().Name + ": " + ex.Message);
            }

            result.CompletedUtc = DateTimeOffset.UtcNow.ToString("o");
            return result;
        }
    }
}
