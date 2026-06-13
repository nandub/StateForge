using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace StateForge.Snapshots
{
    public sealed class StateForgeSnapshotService
    {
        public StateForgeSnapshotResult Create(StateForgeSnapshotOptions options)
        {
            ValidateCreateOptions(options);

            string sourceSessionsPath = ResolveSessionsPath(options.SourceRootPath);
            string snapshotName = string.IsNullOrWhiteSpace(options.SnapshotName)
                ? DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss")
                : options.SnapshotName;

            string snapshotPath = Path.Combine(Path.GetFullPath(options.SnapshotRepositoryPath), snapshotName);
            string snapshotSessionsPath = Path.Combine(snapshotPath, "sessions");

            StateForgeSnapshotResult result = new StateForgeSnapshotResult();
            result.SnapshotName = snapshotName;
            result.SnapshotPath = snapshotPath;
            result.ManifestPath = Path.Combine(snapshotPath, "snapshot-manifest.json");

            if (Directory.Exists(snapshotPath))
            {
                if (!options.OverwriteExisting)
                {
                    result.Errors++;
                    result.Success = false;
                    return result;
                }

                Directory.Delete(snapshotPath, true);
            }

            Directory.CreateDirectory(snapshotSessionsPath);

            StateForgeSnapshotManifest manifest = new StateForgeSnapshotManifest();
            manifest.SnapshotName = snapshotName;
            manifest.CreatedUtc = DateTimeOffset.UtcNow.ToString("o");
            manifest.SourceRootPath = Path.GetFullPath(options.SourceRootPath);
            manifest.SnapshotPath = snapshotPath;

            string[] files = Directory.Exists(sourceSessionsPath)
                ? Directory.GetFiles(sourceSessionsPath, "*.stfg", SearchOption.AllDirectories)
                : new string[0];

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string relative = MakeRelative(sourceSessionsPath, files[i]);
                    string destination = Path.Combine(snapshotSessionsPath, relative);
                    string directory = Path.GetDirectoryName(destination);

                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    FileInfo sourceInfo = new FileInfo(files[i]);
                    File.Copy(files[i], destination, true);
                    File.SetLastWriteTimeUtc(destination, sourceInfo.LastWriteTimeUtc);

                    StateForgeSnapshotManifestEntry entry = new StateForgeSnapshotManifestEntry();
                    entry.RelativePath = relative;
                    entry.Length = sourceInfo.Length;
                    entry.LastWriteUtc = sourceInfo.LastWriteTimeUtc.ToString("o");
                    manifest.Entries.Add(entry);

                    manifest.FileCount++;
                    manifest.TotalBytes += sourceInfo.Length;
                    result.FilesCopied++;
                }
                catch
                {
                    result.Errors++;
                }
            }

            WriteManifest(result.ManifestPath, manifest);
            result.Success = result.Errors == 0;
            return result;
        }

        public StateForgeSnapshotResult Restore(string snapshotPath, string destinationRootPath, bool overwriteExisting)
        {
            StateForgeSnapshotResult result = new StateForgeSnapshotResult();
            string sourceSessionsPath = Path.Combine(Path.GetFullPath(snapshotPath), "sessions");
            string destinationSessionsPath = Path.Combine(Path.GetFullPath(destinationRootPath), "sessions");

            if (!Directory.Exists(sourceSessionsPath))
            {
                result.Errors++;
                result.Success = false;
                return result;
            }

            Directory.CreateDirectory(destinationSessionsPath);

            string[] files = Directory.GetFiles(sourceSessionsPath, "*.stfg", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                try
                {
                    string relative = MakeRelative(sourceSessionsPath, files[i]);
                    string destination = Path.Combine(destinationSessionsPath, relative);
                    string directory = Path.GetDirectoryName(destination);

                    if (File.Exists(destination) && !overwriteExisting)
                    {
                        result.FilesSkipped++;
                        continue;
                    }

                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.Copy(files[i], destination, true);
                    File.SetLastWriteTimeUtc(destination, File.GetLastWriteTimeUtc(files[i]));
                    result.FilesCopied++;
                }
                catch
                {
                    result.Errors++;
                }
            }

            result.SnapshotPath = snapshotPath;
            result.Success = result.Errors == 0;
            return result;
        }

        public string[] List(string snapshotRepositoryPath)
        {
            if (!Directory.Exists(snapshotRepositoryPath))
            {
                return new string[0];
            }

            return Directory.GetDirectories(snapshotRepositoryPath);
        }

        private static void ValidateCreateOptions(StateForgeSnapshotOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (string.IsNullOrWhiteSpace(options.SourceRootPath))
            {
                throw new ArgumentException("SourceRootPath is required.", "options");
            }

            if (string.IsNullOrWhiteSpace(options.SnapshotRepositoryPath))
            {
                throw new ArgumentException("SnapshotRepositoryPath is required.", "options");
            }
        }

        public static string ResolveSessionsPath(string rootPath)
        {
            return Path.Combine(Path.GetFullPath(rootPath), "sessions");
        }

        public static string MakeRelative(string rootPath, string filePath)
        {
            string root = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string full = Path.GetFullPath(filePath);

            if (full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            {
                return full.Substring(root.Length);
            }

            return Path.GetFileName(filePath);
        }

        public static void WriteManifest(string manifestPath, StateForgeSnapshotManifest manifest)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"version\": \"" + Escape(manifest.Version) + "\",");
            builder.AppendLine("  \"snapshotName\": \"" + Escape(manifest.SnapshotName) + "\",");
            builder.AppendLine("  \"createdUtc\": \"" + Escape(manifest.CreatedUtc) + "\",");
            builder.AppendLine("  \"sourceRootPath\": \"" + Escape(manifest.SourceRootPath) + "\",");
            builder.AppendLine("  \"snapshotPath\": \"" + Escape(manifest.SnapshotPath) + "\",");
            builder.AppendLine("  \"fileCount\": " + manifest.FileCount.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"totalBytes\": " + manifest.TotalBytes.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"entries\": [");

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                StateForgeSnapshotManifestEntry entry = manifest.Entries[i];
                builder.AppendLine("    {");
                builder.AppendLine("      \"relativePath\": \"" + Escape(entry.RelativePath) + "\",");
                builder.AppendLine("      \"length\": " + entry.Length.ToString(CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"lastWriteUtc\": \"" + Escape(entry.LastWriteUtc) + "\"");
                builder.Append("    }");

                if (i < manifest.Entries.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");
            File.WriteAllText(manifestPath, builder.ToString(), Encoding.UTF8);
        }

        public static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
