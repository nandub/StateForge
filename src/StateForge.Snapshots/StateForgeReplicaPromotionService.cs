using System;
using System.IO;
using System.Text;
using StateForge.Replication;

namespace StateForge.Snapshots
{
    public sealed class StateForgeReplicaPromotionService
    {
        public StateForgeReplicaPromotionResult Promote(StateForgeReplicaPromotionOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            StateForgeReplicaPromotionResult result = new StateForgeReplicaPromotionResult();
            if (options.RequirePromotionFence && options.PromotionFence == null)
            {
                result.Errors = 1;
                result.Success = false;
                return result;
            }

            if (options.PromotionFence != null)
            {
                StateForgePromotionFenceService fenceService = new StateForgePromotionFenceService();
                result.PromotionFence = fenceService.Acquire(options.PromotionFence);
                if (!result.PromotionFence.Acquired)
                {
                    result.Errors = 1;
                    result.Success = false;
                    return result;
                }
            }

            StateForgeSnapshotService snapshotService = new StateForgeSnapshotService();
            StateForgeSnapshotResult restore = snapshotService.Restore(
                options.ReplicaRootPath,
                options.NewPrimaryRootPath,
                options.OverwriteExisting);

            result.FilesCopied = restore.FilesCopied;
            result.FilesSkipped = restore.FilesSkipped;
            result.Errors = restore.Errors;

            result.Success = result.Errors == 0;

            if (result.Success)
            {
                string markerPath = Path.Combine(Path.GetFullPath(options.NewPrimaryRootPath), "promotion-marker.json");
                WritePromotionMarker(markerPath, options);
                result.PromotionMarkerPath = markerPath;
            }

            return result;
        }

        private static void WritePromotionMarker(string markerPath, StateForgeReplicaPromotionOptions options)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(markerPath));

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"version\": \"0.26.1\",");
            builder.AppendLine("  \"promotedUtc\": \"" + StateForgeSnapshotService.Escape(DateTimeOffset.UtcNow.ToString("o")) + "\",");
            builder.AppendLine("  \"replicaRootPath\": \"" + StateForgeSnapshotService.Escape(options.ReplicaRootPath) + "\",");
            builder.AppendLine("  \"newPrimaryRootPath\": \"" + StateForgeSnapshotService.Escape(options.NewPrimaryRootPath) + "\"");
            builder.AppendLine("}");

            File.WriteAllText(markerPath, builder.ToString(), Encoding.UTF8);
        }
    }
}
