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
            result.Success = result.Errors == 0;

            if (result.Success)
            {
                string markerPath = Path.Combine(Path.GetFullPath(options.NewPrimaryRootPath), "failover-marker.json");
                WriteFailoverMarker(markerPath, options, selectedReplica);
                result.MarkerPath = markerPath;
            }

            return result;
        }

        public bool IsHealthy(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return false;
            }

            string sessionsPath = Path.Combine(Path.GetFullPath(rootPath), "sessions");

            if (!Directory.Exists(sessionsPath))
            {
                return false;
            }

            try
            {
                string[] files = Directory.GetFiles(sessionsPath, "*.stfg", SearchOption.AllDirectories);

                for (int i = 0; i < files.Length; i++)
                {
                    using (FileStream stream = new FileStream(files[i], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (stream.Length < 8)
                        {
                            return false;
                        }

                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            if (reader.ReadInt32() != StateForge.Core.StateForgeConstants.FileMagic ||
                                reader.ReadInt32() != StateForge.Core.StateForgeConstants.FileVersion)
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
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

                StateForgeFailoverService service = new StateForgeFailoverService();

                if (service.IsHealthy(replica))
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
