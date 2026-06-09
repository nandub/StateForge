using System.Collections.Generic;

namespace StateForge.Maintenance.Host
{
    internal sealed class StateForgeMaintenanceHostResult
    {
        public bool Success { get; set; }
        public string RootPath { get; set; }
        public string StartedUtc { get; set; }
        public string CompletedUtc { get; set; }
        public bool CleanupRan { get; set; }
        public int CleanupExpiredDeleted { get; set; }
        public int CleanupInvalidQuarantined { get; set; }
        public int CleanupFailed { get; set; }
        public bool HealthRan { get; set; }
        public bool HealthEvaluated { get; set; }
        public bool Healthy { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanLock { get; set; }
        public bool CanEnumerate { get; set; }
        public bool CanCleanup { get; set; }
        public bool StatsRan { get; set; }
        public int TotalSessions { get; set; }
        public int ExpiredSessions { get; set; }
        public int LockedSessions { get; set; }
        public int CompressedSessions { get; set; }
        public int EncryptedSessions { get; set; }
        public int AesEncryptedSessions { get; set; }
        public long TotalPayloadBytes { get; set; }
        public long AveragePayloadBytes { get; set; }
        public bool MigrationRan { get; set; }
        public int MigrationFilesScanned { get; set; }
        public int MigrationLegacyFilesFound { get; set; }
        public int MigrationStfg2FilesSkipped { get; set; }
        public int MigrationMigratedFiles { get; set; }
        public int MigrationFailedFiles { get; set; }
        public List<string> Errors { get; private set; }

        public StateForgeMaintenanceHostResult()
        {
            Errors = new List<string>();
        }
    }
}
