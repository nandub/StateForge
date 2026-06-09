using System;
using System.IO;
using System.Text;

namespace StateForge.Snapshots
{
    public sealed class StateForgeFailoverService
    {
        public StateForgeFailoverResult EvaluateAndFailover(StateForgeFailoverOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            StateForgeFailoverResult result = new StateForgeFailoverResult();
            result.PrimaryHealthy = IsHealthy(options.PrimaryRootPath);

            if (result.PrimaryHealthy && !options.Force)
            {
                result.Success = true;
                return result;
            }

            string selectedReplica = SelectReplica(options);

            if (string.IsNullOrWhiteSpace(selectedReplica))
            {
                result.Errors++;
                result.Success = false;
                return result;
            }

            StateForgeReplicaPromotionService promotion = new StateForgeReplicaPromotionService();
            StateForgeReplicaPromotionOptions promotionOptions = new StateForgeReplicaPromotionOptions();
            promotionOptions.ReplicaRootPath = selectedReplica;
            promotionOptions.NewPrimaryRootPath = options.NewPrimaryRootPath;
            promotionOptions.OverwriteExisting = true;

            StateForgeReplicaPromotionResult promotionResult = promotion.Promote(promotionOptions);
            result.Errors = promotionResult.Errors;
            result.PromotedReplicaRootPath = selectedReplica;

            string markerPath = Path.Combine(Path.GetFullPath(options.NewPrimaryRootPath), "failover-marker.json");
            WriteFailoverMarker(markerPath, options, selectedReplica);

            result.MarkerPath = markerPath;
            result.Success = result.Errors == 0;
            return result;
        }

        public bool IsHealthy(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return false;
            }

            string sessionsPath = Path.Combine(Path.GetFullPath(rootPath), "sessions");
            return Directory.Exists(sessionsPath);
        }

        private static string SelectReplica(StateForgeFailoverOptions options)
        {
            for (int i = 0; i < options.ReplicaRootPaths.Count; i++)
            {
                string replica = options.ReplicaRootPaths[i];

                if (string.IsNullOrWhiteSpace(replica))
                {
                    continue;
                }

                string sessions = Path.Combine(Path.GetFullPath(replica), "sessions");

                if (Directory.Exists(sessions))
                {
                    return replica;
                }
            }

            return null;
        }

        private static void WriteFailoverMarker(string markerPath, StateForgeFailoverOptions options, string selectedReplica)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(markerPath));

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"version\": \"0.26.1\",");
            builder.AppendLine("  \"failedOverUtc\": \"" + StateForgeSnapshotService.Escape(DateTimeOffset.UtcNow.ToString("o")) + "\",");
            builder.AppendLine("  \"primaryRootPath\": \"" + StateForgeSnapshotService.Escape(options.PrimaryRootPath) + "\",");
            builder.AppendLine("  \"promotedReplicaRootPath\": \"" + StateForgeSnapshotService.Escape(selectedReplica) + "\"");
            builder.AppendLine("}");

            File.WriteAllText(markerPath, builder.ToString(), Encoding.UTF8);
        }
    }
}
