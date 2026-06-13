using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace StateForge.Replication
{
    public sealed class StateForgeFileReplicator
    {
        public StateForgeReplicationResult Replicate(StateForgeReplicationOptions options)
        {
            StateForgeReplicationPlan plan = StateForgeReplicationPlanner.CreatePlan(options);
            StateForgeReplicationResult result = new StateForgeReplicationResult();
            result.DryRun = options.DryRun;

            StateForgeReplicationManifest manifest = new StateForgeReplicationManifest();
            manifest.CapturedUtc = DateTimeOffset.UtcNow.ToString("o");
            manifest.PrimaryRootPath = plan.PrimaryRootPath;
            manifest.PrimarySessionsPath = plan.PrimarySessionsPath;
            result.Manifest = manifest;

            if (!Directory.Exists(plan.PrimarySessionsPath))
            {
                result.Errors++;
                result.Messages.Add("Primary sessions path does not exist: " + plan.PrimarySessionsPath);

                if (!options.DryRun)
                {
                    for (int i = 0; i < plan.Targets.Count; i++)
                    {
                        RecordState(
                            plan.Targets[i],
                            false,
                            "Primary sessions path does not exist.",
                            result);
                    }
                }

                return result;
            }

            string[] files = Directory.GetFiles(plan.PrimarySessionsPath, "*.stfg", SearchOption.AllDirectories);
            result.SourceFilesScanned = files.Length;

            for (int t = 0; t < plan.Targets.Count; t++)
            {
                StateForgeReplicationTarget target = plan.Targets[t];
                result.ReplicasVisited++;
                int errorsBefore = result.Errors;
                int conflictsBefore = result.Conflicts;

                for (int i = 0; i < files.Length; i++)
                {
                    TryReplicateFile(options, plan, target, files[i], result, manifest);
                }

                if (!options.DryRun)
                {
                    bool targetSuccess = result.Errors == errorsBefore &&
                        result.Conflicts == conflictsBefore;
                    string error = targetSuccess
                        ? string.Empty
                        : "Replication completed with copy errors or conflicts.";
                    RecordState(target, targetSuccess, error, result);
                }
            }

            if (!string.IsNullOrWhiteSpace(options.ManifestPath))
            {
                WriteManifest(options.ManifestPath, manifest);
                result.ManifestPath = Path.GetFullPath(options.ManifestPath);
            }

            return result;
        }

        private static void RecordState(
            StateForgeReplicationTarget target,
            bool success,
            string error,
            StateForgeReplicationResult result)
        {
            try
            {
                StateForgeReplicaStateStore.RecordReplication(
                    target.RootPath,
                    target.Name,
                    success,
                    error,
                    DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                result.Errors++;
                result.Messages.Add(
                    "Replica monitoring state write failed for " + target.Name + ": " +
                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void TryReplicateFile(
            StateForgeReplicationOptions options,
            StateForgeReplicationPlan plan,
            StateForgeReplicationTarget target,
            string sourceFile,
            StateForgeReplicationResult result,
            StateForgeReplicationManifest manifest)
        {
            try
            {
                string relative = MakeRelative(plan.PrimarySessionsPath, sourceFile);
                string destination = Path.Combine(target.SessionsPath, relative);
                string directory = Path.GetDirectoryName(destination);

                FileInfo sourceInfo = new FileInfo(sourceFile);
                FileInfo destinationInfo = new FileInfo(destination);

                StateForgeReplicationManifestEntry entry = new StateForgeReplicationManifestEntry();
                entry.RelativePath = relative;
                entry.SourceLength = sourceInfo.Length;
                entry.SourceLastWriteUtc = sourceInfo.LastWriteTimeUtc.ToString("o");
                entry.ReplicaName = target.Name;
                entry.DestinationPath = destination;

                if (destinationInfo.Exists)
                {
                    if (options.DetectConflicts && IsConflict(sourceInfo, destinationInfo))
                    {
                        result.Conflicts++;
                        result.FilesSkipped++;
                        entry.Action = "conflict";
                        entry.Reason = "Destination exists with different length or newer timestamp.";
                        manifest.Entries.Add(entry);
                        return;
                    }

                    if (!options.OverwriteExisting)
                    {
                        result.FilesSkipped++;
                        entry.Action = "skip";
                        entry.Reason = "Destination exists and overwrite is disabled.";
                        manifest.Entries.Add(entry);
                        return;
                    }
                }

                if (options.DryRun)
                {
                    result.FilesSkipped++;
                    entry.Action = "dry-run";
                    entry.Reason = "Dry run; file not copied.";
                    manifest.Entries.Add(entry);
                    return;
                }

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(sourceFile, destination, true);
                File.SetLastWriteTimeUtc(destination, sourceInfo.LastWriteTimeUtc);

                result.FilesCopied++;
                entry.Action = "copy";
                entry.Reason = "Copied.";
                manifest.Entries.Add(entry);
            }
            catch (Exception ex)
            {
                result.Errors++;
                result.Messages.Add(ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static bool IsConflict(FileInfo source, FileInfo destination)
        {
            if (!destination.Exists)
            {
                return false;
            }

            if (source.Length != destination.Length)
            {
                return true;
            }

            return destination.LastWriteTimeUtc > source.LastWriteTimeUtc.AddSeconds(1);
        }

        private static string MakeRelative(string rootPath, string filePath)
        {
            string root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(filePath);

            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return full.Substring(root.Length);
            }

            return Path.GetFileName(filePath);
        }

        public static void WriteManifest(string manifestPath, StateForgeReplicationManifest manifest)
        {
            string fullPath = Path.GetFullPath(manifestPath);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"version\": \"0.22.1\",");
            builder.AppendLine("  \"capturedUtc\": \"" + Escape(manifest.CapturedUtc) + "\",");
            builder.AppendLine("  \"primaryRootPath\": \"" + Escape(manifest.PrimaryRootPath) + "\",");
            builder.AppendLine("  \"primarySessionsPath\": \"" + Escape(manifest.PrimarySessionsPath) + "\",");
            builder.AppendLine("  \"entries\": [");

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                StateForgeReplicationManifestEntry entry = manifest.Entries[i];

                builder.AppendLine("    {");
                builder.AppendLine("      \"relativePath\": \"" + Escape(entry.RelativePath) + "\",");
                builder.AppendLine("      \"sourceLength\": " + entry.SourceLength.ToString(CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"sourceLastWriteUtc\": \"" + Escape(entry.SourceLastWriteUtc) + "\",");
                builder.AppendLine("      \"replicaName\": \"" + Escape(entry.ReplicaName) + "\",");
                builder.AppendLine("      \"destinationPath\": \"" + Escape(entry.DestinationPath) + "\",");
                builder.AppendLine("      \"action\": \"" + Escape(entry.Action) + "\",");
                builder.AppendLine("      \"reason\": \"" + Escape(entry.Reason) + "\"");
                builder.Append("    }");

                if (i < manifest.Entries.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(fullPath, builder.ToString(), Encoding.UTF8);
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
