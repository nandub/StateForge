using System;
using System.IO;
using System.Text;

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

            StateForgeSnapshotService snapshotService = new StateForgeSnapshotService();
            StateForgeSnapshotResult restore = snapshotService.Restore(
                options.ReplicaRootPath,
                options.NewPrimaryRootPath,
                options.OverwriteExisting);

            StateForgeReplicaPromotionResult result = new StateForgeReplicaPromotionResult();
            result.FilesCopied = restore.FilesCopied;
            result.FilesSkipped = restore.FilesSkipped;
            result.Errors = restore.Errors;

            string markerPath = Path.Combine(Path.GetFullPath(options.NewPrimaryRootPath), "promotion-marker.json");
            WritePromotionMarker(markerPath, options);

            result.PromotionMarkerPath = markerPath;
            result.Success = result.Errors == 0;
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
