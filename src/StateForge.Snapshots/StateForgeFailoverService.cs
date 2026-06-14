using System;
using System.IO;
using System.Text;

namespace StateForge.Snapshots
{
    /// <summary>Evaluates primary health and performs fenced replica promotion when required.</summary>
    public sealed class StateForgeFailoverService
    {
        /// <summary>Evaluates the primary and promotes an eligible replica when failover is required.</summary>
        /// <param name="options">Health, replica, fencing, cross-site, and destination settings.</param>
        /// <returns>The health decision, promoted replica, marker path, and any errors.</returns>
        public StateForgeFailoverResult EvaluateAndFailover(StateForgeFailoverOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            StateForgeFailoverResult result = new StateForgeFailoverResult();
            result.PrimaryHealthy = IsHealthy(options.PrimaryRootPath);
            result.CrossSitePolicy = options.CrossSitePolicy;

            if (result.PrimaryHealthy && !options.Force)
            {
                result.Success = true;
                return result;
            }

            string selectedReplica = SelectReplica(options);

            if (options.RequireCrossSitePolicy &&
                (options.CrossSitePolicy == null || !options.CrossSitePolicy.Eligible))
            {
                result.Errors++;
                result.Success = false;
                return result;
            }

            if (options.CrossSitePolicy != null)
            {
                string selectedRoot = string.IsNullOrWhiteSpace(selectedReplica)
                    ? string.Empty
                    : Path.GetFullPath(selectedReplica);
                string policyRoot = string.IsNullOrWhiteSpace(options.CrossSitePolicy.TargetRootPath)
                    ? string.Empty
                    : Path.GetFullPath(options.CrossSitePolicy.TargetRootPath);
                if (!string.Equals(selectedRoot, policyRoot, StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors++;
                    result.Success = false;
                    return result;
                }

                if (options.PromotionFence != null &&
                    !string.Equals(
                        options.CrossSitePolicy.CandidateName,
                        options.PromotionFence.CandidateName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors++;
                    result.Success = false;
                    return result;
                }
            }

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
            promotionOptions.RequirePromotionFence = options.RequirePromotionFence;
            promotionOptions.PromotionFence = options.PromotionFence;

            StateForgeReplicaPromotionResult promotionResult = promotion.Promote(promotionOptions);
            result.Errors = promotionResult.Errors;
            result.PromotionFence = promotionResult.PromotionFence;
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

        /// <summary>Determines whether a store has a readable sessions directory and valid STFG records.</summary>
        /// <param name="rootPath">The candidate store root path.</param>
        /// <returns><see langword="true"/> when the store passes the failover health probe.</returns>
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
            builder.AppendLine("  \"promotedReplicaRootPath\": \"" + StateForgeSnapshotService.Escape(selectedReplica) + "\",");
            builder.AppendLine("  \"sourceSiteName\": \"" +
                StateForgeSnapshotService.Escape(options.CrossSitePolicy == null ? string.Empty : options.CrossSitePolicy.SourceSiteName) + "\",");
            builder.AppendLine("  \"targetSiteName\": \"" +
                StateForgeSnapshotService.Escape(options.CrossSitePolicy == null ? string.Empty : options.CrossSitePolicy.TargetSiteName) + "\"");
            builder.AppendLine("}");

            File.WriteAllText(markerPath, builder.ToString(), Encoding.UTF8);
        }
    }
}
