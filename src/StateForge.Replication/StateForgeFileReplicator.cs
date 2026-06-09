using System;
using System.IO;

namespace StateForge.Replication
{
    public sealed class StateForgeFileReplicator
    {
        public StateForgeReplicationResult Replicate(StateForgeReplicationOptions options)
        {
            StateForgeReplicationPlan plan = StateForgeReplicationPlanner.CreatePlan(options);
            StateForgeReplicationResult result = new StateForgeReplicationResult();

            if (!Directory.Exists(plan.PrimarySessionsPath))
            {
                result.Errors++;
                result.Messages.Add("Primary sessions path does not exist: " + plan.PrimarySessionsPath);
                return result;
            }

            string[] files = Directory.GetFiles(plan.PrimarySessionsPath, "*.stfg", SearchOption.AllDirectories);
            result.SourceFilesScanned = files.Length;

            for (int t = 0; t < plan.Targets.Count; t++)
            {
                StateForgeReplicationTarget target = plan.Targets[t];
                result.ReplicasVisited++;

                for (int i = 0; i < files.Length; i++)
                {
                    try
                    {
                        string sourceFile = files[i];
                        string relative = MakeRelative(plan.PrimarySessionsPath, sourceFile);
                        string destination = Path.Combine(target.SessionsPath, relative);
                        string directory = Path.GetDirectoryName(destination);

                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        if (File.Exists(destination) && !options.OverwriteExisting)
                        {
                            result.FilesSkipped++;
                            continue;
                        }

                        File.Copy(sourceFile, destination, true);
                        result.FilesCopied++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors++;
                        result.Messages.Add(ex.GetType().Name + ": " + ex.Message);
                    }
                }
            }

            return result;
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
    }
}
