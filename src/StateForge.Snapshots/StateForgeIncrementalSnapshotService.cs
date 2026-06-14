using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace StateForge.Snapshots
{
    /// <summary>Creates and restores full-plus-incremental snapshot chains.</summary>
    /// <remarks>File changes are detected with SHA-256 content hashes, including equal-length modifications.</remarks>
    public sealed class StateForgeIncrementalSnapshotService
    {
        /// <summary>Creates the full snapshot that anchors an incremental chain.</summary>
        /// <param name="options">The base snapshot settings.</param>
        /// <returns>The full snapshot result.</returns>
        public StateForgeSnapshotResult CreateBase(StateForgeSnapshotOptions options)
        {
            StateForgeSnapshotService service = new StateForgeSnapshotService();
            return service.Create(options);
        }

        /// <summary>Creates a delta containing added, modified, and deleted-file entries.</summary>
        /// <param name="options">The source, parent, repository, and overwrite settings.</param>
        /// <returns>The incremental snapshot result and change counts.</returns>
        public StateForgeIncrementalSnapshotResult CreateIncremental(StateForgeIncrementalSnapshotOptions options)
        {
            ValidateOptions(options);

            string repositoryPath = Path.GetFullPath(options.SnapshotRepositoryPath);
            string sourceSessionsPath = StateForgeSnapshotService.ResolveSessionsPath(options.SourceRootPath);
            string parentSnapshotPath = StateForgeSnapshotPath.ResolveChildName(
                repositoryPath,
                options.ParentSnapshotName,
                "ParentSnapshotName");
            string parentSessionsPath = Path.Combine(parentSnapshotPath, "sessions");
            string snapshotName = string.IsNullOrWhiteSpace(options.SnapshotName)
                ? DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss")
                : options.SnapshotName;
            string snapshotPath = StateForgeSnapshotPath.ResolveChildName(
                repositoryPath,
                snapshotName,
                "SnapshotName");
            string deltaPath = Path.Combine(snapshotPath, "delta");

            StateForgeIncrementalSnapshotResult result = new StateForgeIncrementalSnapshotResult();
            result.SnapshotName = snapshotName;
            result.SnapshotPath = snapshotPath;
            result.ManifestPath = Path.Combine(snapshotPath, "incremental-manifest.json");

            if (!Directory.Exists(parentSessionsPath))
            {
                result.Errors++;
                result.Success = false;
                return result;
            }

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

            Directory.CreateDirectory(deltaPath);

            Dictionary<string, FileSignature> parentFiles = BuildSignatureMap(parentSessionsPath);
            Dictionary<string, FileSignature> sourceFiles = BuildSignatureMap(sourceSessionsPath);

            StateForgeIncrementalSnapshotManifest manifest = new StateForgeIncrementalSnapshotManifest();
            manifest.SnapshotName = snapshotName;
            manifest.ParentSnapshotName = options.ParentSnapshotName;
            manifest.CreatedUtc = DateTimeOffset.UtcNow.ToString("o");
            manifest.SourceRootPath = Path.GetFullPath(options.SourceRootPath);
            manifest.SnapshotPath = snapshotPath;

            foreach (KeyValuePair<string, FileSignature> source in sourceFiles)
            {
                FileSignature parent;

                if (!parentFiles.TryGetValue(source.Key, out parent))
                {
                    AddCopyEntry("add", source.Key, source.Value, sourceSessionsPath, deltaPath, manifest, result);
                    result.FilesAdded++;
                    continue;
                }

                if (parent.Length != source.Value.Length ||
                    !string.Equals(parent.Hash, source.Value.Hash, StringComparison.Ordinal))
                {
                    AddCopyEntry("modify", source.Key, source.Value, sourceSessionsPath, deltaPath, manifest, result);
                    result.FilesModified++;
                }
            }

            foreach (KeyValuePair<string, FileSignature> parent in parentFiles)
            {
                if (!sourceFiles.ContainsKey(parent.Key))
                {
                    StateForgeIncrementalSnapshotEntry entry = new StateForgeIncrementalSnapshotEntry();
                    entry.RelativePath = parent.Key;
                    entry.Action = "delete";
                    entry.Length = 0;
                    entry.LastWriteUtc = DateTimeOffset.UtcNow.ToString("o");
                    manifest.Entries.Add(entry);
                    result.FilesDeleted++;
                }
            }

            manifest.FilesAdded = result.FilesAdded;
            manifest.FilesModified = result.FilesModified;
            manifest.FilesDeleted = result.FilesDeleted;

            WriteManifest(result.ManifestPath, manifest);
            result.Success = result.Errors == 0;
            return result;
        }

        /// <summary>Restores a base snapshot and replays the named incremental snapshots in order.</summary>
        /// <param name="snapshotRepositoryPath">The snapshot repository root.</param>
        /// <param name="baseSnapshotName">The base snapshot name.</param>
        /// <param name="incrementalSnapshotNames">Incremental snapshot names in replay order.</param>
        /// <param name="destinationRootPath">The destination StateForge root.</param>
        /// <returns>The aggregate restore result.</returns>
        public StateForgeSnapshotResult RestoreChain(string snapshotRepositoryPath, string baseSnapshotName, string[] incrementalSnapshotNames, string destinationRootPath)
        {
            StateForgeSnapshotService service = new StateForgeSnapshotService();
            string repositoryPath = Path.GetFullPath(snapshotRepositoryPath);
            string baseSnapshotPath = StateForgeSnapshotPath.ResolveChildName(
                repositoryPath,
                baseSnapshotName,
                "BaseSnapshotName");
            StateForgeSnapshotResult restore = service.Restore(baseSnapshotPath, destinationRootPath, true);

            if (!restore.Success)
            {
                return restore;
            }

            string destinationSessionsPath = Path.Combine(Path.GetFullPath(destinationRootPath), "sessions");

            if (incrementalSnapshotNames != null)
            {
                for (int i = 0; i < incrementalSnapshotNames.Length; i++)
                {
                    string incrementalPath = StateForgeSnapshotPath.ResolveChildName(
                        repositoryPath,
                        incrementalSnapshotNames[i],
                        "IncrementalSnapshotName");
                    ApplyIncremental(incrementalPath, destinationSessionsPath, restore);
                }
            }

            restore.Success = restore.Errors == 0;
            return restore;
        }

        private static void ApplyIncremental(string snapshotPath, string destinationSessionsPath, StateForgeSnapshotResult result)
        {
            string manifestPath = Path.Combine(snapshotPath, "incremental-manifest.json");

            if (!File.Exists(manifestPath))
            {
                result.Errors++;
                return;
            }

            StateForgeIncrementalSnapshotManifest manifest = ReadManifest(manifestPath);
            string deltaPath = Path.Combine(snapshotPath, "delta");

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                StateForgeIncrementalSnapshotEntry entry = manifest.Entries[i];
                string destination = StateForgeSnapshotPath.ResolveRelativePath(
                    destinationSessionsPath,
                    entry.RelativePath,
                    "Incremental manifest relativePath");

                if (string.Equals(entry.Action, "delete", StringComparison.OrdinalIgnoreCase))
                {
                    if (File.Exists(destination))
                    {
                        File.Delete(destination);
                    }

                    continue;
                }

                string source = StateForgeSnapshotPath.ResolveRelativePath(
                    deltaPath,
                    entry.RelativePath,
                    "Incremental manifest relativePath");
                string directory = Path.GetDirectoryName(destination);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(source, destination, true);
                result.FilesCopied++;
            }
        }

        private static void AddCopyEntry(string action, string relativePath, FileSignature signature, string sourceSessionsPath, string deltaPath, StateForgeIncrementalSnapshotManifest manifest, StateForgeIncrementalSnapshotResult result)
        {
            try
            {
                string sourceFile = Path.Combine(sourceSessionsPath, relativePath);
                string destination = Path.Combine(deltaPath, relativePath);
                string directory = Path.GetDirectoryName(destination);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.Copy(sourceFile, destination, true);
                File.SetLastWriteTimeUtc(destination, DateTimeOffset.Parse(signature.LastWriteUtc).UtcDateTime);

                StateForgeIncrementalSnapshotEntry entry = new StateForgeIncrementalSnapshotEntry();
                entry.RelativePath = relativePath;
                entry.Action = action;
                entry.Length = signature.Length;
                entry.LastWriteUtc = signature.LastWriteUtc;
                manifest.Entries.Add(entry);
                result.FilesCopied++;
            }
            catch
            {
                result.Errors++;
            }
        }

        private static Dictionary<string, FileSignature> BuildSignatureMap(string sessionsPath)
        {
            Dictionary<string, FileSignature> map = new Dictionary<string, FileSignature>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(sessionsPath))
            {
                return map;
            }

            string[] files = Directory.GetFiles(sessionsPath, "*.stfg", SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                FileInfo info = new FileInfo(files[i]);
                FileSignature signature = new FileSignature();
                signature.Length = info.Length;
                signature.LastWriteUtc = info.LastWriteTimeUtc.ToString("o");
                signature.Hash = ComputeSha256(info.FullName);
                map[StateForgeSnapshotService.MakeRelative(sessionsPath, files[i])] = signature;
            }

            return map;
        }

        private static void ValidateOptions(StateForgeIncrementalSnapshotOptions options)
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

            if (string.IsNullOrWhiteSpace(options.ParentSnapshotName))
            {
                throw new ArgumentException("ParentSnapshotName is required.", "options");
            }
        }

        /// <summary>Writes an incremental snapshot manifest as UTF-8 JSON.</summary>
        public static void WriteManifest(string manifestPath, StateForgeIncrementalSnapshotManifest manifest)
        {
            string directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"version\": \"" + StateForgeSnapshotService.Escape(manifest.Version) + "\",");
            builder.AppendLine("  \"snapshotName\": \"" + StateForgeSnapshotService.Escape(manifest.SnapshotName) + "\",");
            builder.AppendLine("  \"snapshotType\": \"" + StateForgeSnapshotService.Escape(manifest.SnapshotType) + "\",");
            builder.AppendLine("  \"parentSnapshotName\": \"" + StateForgeSnapshotService.Escape(manifest.ParentSnapshotName) + "\",");
            builder.AppendLine("  \"createdUtc\": \"" + StateForgeSnapshotService.Escape(manifest.CreatedUtc) + "\",");
            builder.AppendLine("  \"sourceRootPath\": \"" + StateForgeSnapshotService.Escape(manifest.SourceRootPath) + "\",");
            builder.AppendLine("  \"snapshotPath\": \"" + StateForgeSnapshotService.Escape(manifest.SnapshotPath) + "\",");
            builder.AppendLine("  \"filesAdded\": " + manifest.FilesAdded.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"filesModified\": " + manifest.FilesModified.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"filesDeleted\": " + manifest.FilesDeleted.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"entries\": [");

            for (int i = 0; i < manifest.Entries.Count; i++)
            {
                StateForgeIncrementalSnapshotEntry entry = manifest.Entries[i];
                builder.AppendLine("    {");
                builder.AppendLine("      \"relativePath\": \"" + StateForgeSnapshotService.Escape(entry.RelativePath) + "\",");
                builder.AppendLine("      \"action\": \"" + StateForgeSnapshotService.Escape(entry.Action) + "\",");
                builder.AppendLine("      \"length\": " + entry.Length.ToString(CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"lastWriteUtc\": \"" + StateForgeSnapshotService.Escape(entry.LastWriteUtc) + "\"");
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

        /// <summary>Reads an incremental snapshot manifest.</summary>
        public static StateForgeIncrementalSnapshotManifest ReadManifest(string manifestPath)
        {
            string json = File.ReadAllText(manifestPath);
            StateForgeIncrementalSnapshotManifest manifest = new StateForgeIncrementalSnapshotManifest();
            manifest.SnapshotName = ReadString(json, "snapshotName");
            manifest.SnapshotType = ReadString(json, "snapshotType");
            manifest.ParentSnapshotName = ReadString(json, "parentSnapshotName");
            manifest.CreatedUtc = ReadString(json, "createdUtc");
            manifest.SourceRootPath = ReadString(json, "sourceRootPath");
            manifest.SnapshotPath = ReadString(json, "snapshotPath");
            manifest.FilesAdded = ReadInt(json, "filesAdded");
            manifest.FilesModified = ReadInt(json, "filesModified");
            manifest.FilesDeleted = ReadInt(json, "filesDeleted");

            MatchCollection matches = Regex.Matches(json, "\\{\\s*\\\"relativePath\\\"\\s*:\\s*\\\"(?<path>[^\\\"]*)\\\"\\s*,\\s*\\\"action\\\"\\s*:\\s*\\\"(?<action>[^\\\"]*)\\\"\\s*,\\s*\\\"length\\\"\\s*:\\s*(?<length>\\d+)\\s*,\\s*\\\"lastWriteUtc\\\"\\s*:\\s*\\\"(?<last>[^\\\"]*)\\\"\\s*\\}", RegexOptions.IgnoreCase);

            for (int i = 0; i < matches.Count; i++)
            {
                StateForgeIncrementalSnapshotEntry entry = new StateForgeIncrementalSnapshotEntry();
                entry.RelativePath = Unescape(matches[i].Groups["path"].Value);
                entry.Action = matches[i].Groups["action"].Value;
                entry.Length = long.Parse(matches[i].Groups["length"].Value, CultureInfo.InvariantCulture);
                entry.LastWriteUtc = matches[i].Groups["last"].Value;
                manifest.Entries.Add(entry);
            }

            return manifest;
        }

        private static string ReadString(string json, string name)
        {
            Match match = Regex.Match(json, "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*\\\"(?<value>[^\\\"]*)\\\"", RegexOptions.IgnoreCase);
            return match.Success ? Unescape(match.Groups["value"].Value) : string.Empty;
        }

        private static int ReadInt(string json, string name)
        {
            Match match = Regex.Match(json, "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*(?<value>\\d+)", RegexOptions.IgnoreCase);
            int value;
            return match.Success && int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : 0;
        }

        private static string Unescape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Replace("\\\\", "\\").Replace("\\\"", "\"");
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(hash.Length * 2);

                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        private sealed class FileSignature
        {
            public long Length { get; set; }
            public string LastWriteUtc { get; set; }
            public string Hash { get; set; }
        }
    }
}
