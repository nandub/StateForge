using System;
using System.IO;

namespace StateForge.Performance
{
    public static class StateForgeShardAnalyzer
    {
        public static StateForgeShardAnalysisResult Analyze(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path is required.", "rootPath");
            }

            StateForgeShardAnalysisResult result = new StateForgeShardAnalysisResult();
            result.RootPath = rootPath;

            string sessionsPath = Path.Combine(rootPath, "sessions");
            result.SessionsPath = sessionsPath;

            if (!Directory.Exists(rootPath))
            {
                result.Warnings.Add("Root path does not exist.");
                return result;
            }

            if (!Directory.Exists(sessionsPath))
            {
                sessionsPath = rootPath;
                result.SessionsPath = sessionsPath;
            }

            string[] directories = Directory.GetDirectories(sessionsPath);
            string[] rootFiles = Directory.GetFiles(sessionsPath, "*.stfg", SearchOption.TopDirectoryOnly);

            result.DirectoryCount = directories.Length;
            result.FileCount = rootFiles.Length;

            int totalShardFiles = 0;
            int max = 0;
            int min = int.MaxValue;

            for (int i = 0; i < directories.Length; i++)
            {
                string[] files = Directory.GetFiles(directories[i], "*.stfg", SearchOption.TopDirectoryOnly);
                int count = files.Length;
                totalShardFiles += count;

                if (count > max)
                {
                    max = count;
                }

                if (count < min)
                {
                    min = count;
                }
            }

            result.FileCount += totalShardFiles;
            result.MaxFilesPerDirectory = max;
            result.MinFilesPerDirectory = min == int.MaxValue ? 0 : min;

            if (directories.Length > 0)
            {
                result.AverageFilesPerDirectory = totalShardFiles / (double)directories.Length;
            }

            result.AppearsSharded = directories.Length > 0 && rootFiles.Length == 0;

            if (rootFiles.Length > 0)
            {
                result.Warnings.Add("Session files exist directly under the sessions/root directory; sharding may be incomplete.");
            }

            if (result.MaxFilesPerDirectory > 5000)
            {
                result.Warnings.Add("One or more shard directories contain more than 5000 files.");
            }

            if (directories.Length == 0 && result.FileCount > 0)
            {
                result.Warnings.Add("No shard directories were found.");
            }

            return result;
        }
    }
}
