using System;
using System.IO;

namespace StateForge.Snapshots
{
    public sealed class StateForgeSnapshotScheduler
    {
        public StateForgeSnapshotResult RunOnce(StateForgeSnapshotScheduleOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            StateForgeSnapshotService service = new StateForgeSnapshotService();
            StateForgeSnapshotOptions snapshot = new StateForgeSnapshotOptions();
            snapshot.SourceRootPath = options.SourceRootPath;
            snapshot.SnapshotRepositoryPath = options.SnapshotRepositoryPath;
            snapshot.SnapshotName = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            snapshot.OverwriteExisting = false;

            StateForgeSnapshotResult result = service.Create(snapshot);
            ApplyRetention(options);
            return result;
        }

        public void ApplyRetention(StateForgeSnapshotScheduleOptions options)
        {
            if (options.RetainLast <= 0 || !Directory.Exists(options.SnapshotRepositoryPath))
            {
                return;
            }

            DirectoryInfo repository = new DirectoryInfo(options.SnapshotRepositoryPath);
            DirectoryInfo[] snapshots = repository.GetDirectories();
            Array.Sort(snapshots, delegate(DirectoryInfo left, DirectoryInfo right)
            {
                return string.Compare(right.Name, left.Name, StringComparison.OrdinalIgnoreCase);
            });

            for (int i = options.RetainLast; i < snapshots.Length; i++)
            {
                snapshots[i].Delete(true);
            }
        }
    }
}
