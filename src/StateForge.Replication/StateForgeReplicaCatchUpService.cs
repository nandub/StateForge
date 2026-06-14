using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StateForge.Replication
{
    /// <summary>Plans and applies deterministic file-level replica resynchronization.</summary>
    /// <remarks>Changed-file detection uses SHA-256 content hashes and does not rely on timestamps or file length alone.</remarks>
    public sealed class StateForgeReplicaCatchUpService
    {
        /// <summary>Compares primary and replica content without modifying either store.</summary>
        /// <param name="options">Primary, replica, and deletion-policy settings.</param>
        /// <returns>A dry-run plan containing missing, changed, and extra files.</returns>
        public StateForgeReplicaCatchUpResult Plan(StateForgeReplicaCatchUpOptions options)
        {
            ValidateOptions(options);

            StateForgeReplicaCatchUpResult result = new StateForgeReplicaCatchUpResult();
            result.DryRun = true;

            string primarySessions = ResolveSessionsPath(options.PrimaryRootPath);
            string replicaSessions = ResolveSessionsPath(options.ReplicaRootPath);

            Dictionary<string, FileSignature> primaryFiles = BuildSignatureMap(primarySessions);
            Dictionary<string, FileSignature> replicaFiles = BuildSignatureMap(replicaSessions);

            foreach (KeyValuePair<string, FileSignature> primary in primaryFiles)
            {
                FileSignature replica;
                if (!replicaFiles.TryGetValue(primary.Key, out replica))
                {
                    AddEntry(result, primary.Key, "copy-missing", primary.Value.Length, -1);
                    result.MissingFiles++;
                    continue;
                }

                if (primary.Value.Length != replica.Length ||
                    !string.Equals(primary.Value.Hash, replica.Hash, StringComparison.Ordinal))
                {
                    AddEntry(result, primary.Key, "copy-changed", primary.Value.Length, replica.Length);
                    result.ChangedFiles++;
                }
            }

            foreach (KeyValuePair<string, FileSignature> replica in replicaFiles)
            {
                if (!primaryFiles.ContainsKey(replica.Key))
                {
                    AddEntry(result, replica.Key, "delete-extra", -1, replica.Value.Length);
                    result.ExtraFiles++;
                }
            }

            result.Success = true;
            return result;
        }

        /// <summary>Applies a catch-up plan to converge the replica with the primary.</summary>
        /// <param name="options">Primary, replica, dry-run, and deletion-policy settings.</param>
        /// <returns>The plan and resulting copy, deletion, and error counts.</returns>
        public StateForgeReplicaCatchUpResult Apply(StateForgeReplicaCatchUpOptions options)
        {
            ValidateOptions(options);

            StateForgeReplicaCatchUpResult plan = Plan(options);
            plan.DryRun = options.DryRun;

            if (options.DryRun)
            {
                return plan;
            }

            string primarySessions = ResolveSessionsPath(options.PrimaryRootPath);
            string replicaSessions = ResolveSessionsPath(options.ReplicaRootPath);

            for (int i = 0; i < plan.Entries.Count; i++)
            {
                StateForgeReplicaCatchUpEntry entry = plan.Entries[i];

                try
                {
                    if (string.Equals(entry.Action, "copy-missing", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(entry.Action, "copy-changed", StringComparison.OrdinalIgnoreCase))
                    {
                        CopyReplicaFile(primarySessions, replicaSessions, entry.RelativePath);
                        plan.CopiedFiles++;
                    }
                    else if (string.Equals(entry.Action, "delete-extra", StringComparison.OrdinalIgnoreCase) &&
                             options.DeleteExtraReplicaFiles)
                    {
                        string replicaPath = Path.Combine(replicaSessions, entry.RelativePath);
                        if (File.Exists(replicaPath))
                        {
                            File.Delete(replicaPath);
                            plan.DeletedFiles++;
                        }
                    }
                }
                catch
                {
                    plan.Errors++;
                }
            }

            plan.Success = plan.Errors == 0;

            try
            {
                StateForgeReplicaStateStore.RecordCatchUp(
                    options.ReplicaRootPath,
                    ResolveReplicaName(options),
                    plan.Success,
                    plan.Success ? string.Empty : "Replica catch-up completed with errors.",
                    DateTimeOffset.UtcNow);
            }
            catch
            {
                plan.Errors++;
                plan.Success = false;
            }

            return plan;
        }

        private static void AddEntry(StateForgeReplicaCatchUpResult result, string relativePath, string action, long primaryLength, long replicaLength)
        {
            StateForgeReplicaCatchUpEntry entry = new StateForgeReplicaCatchUpEntry();
            entry.RelativePath = relativePath;
            entry.Action = action;
            entry.PrimaryLength = primaryLength;
            entry.ReplicaLength = replicaLength;
            result.Entries.Add(entry);
        }

        private static void CopyReplicaFile(string primarySessions, string replicaSessions, string relativePath)
        {
            string source = Path.Combine(primarySessions, relativePath);
            string destination = Path.Combine(replicaSessions, relativePath);
            string directory = Path.GetDirectoryName(destination);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.Copy(source, destination, true);
            File.SetLastWriteTimeUtc(destination, File.GetLastWriteTimeUtc(source));
        }

        private static Dictionary<string, FileSignature> BuildSignatureMap(string sessionsPath)
        {
            Dictionary<string, FileSignature> files = new Dictionary<string, FileSignature>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(sessionsPath))
            {
                return files;
            }

            string[] paths = Directory.GetFiles(sessionsPath, "*.stfg", SearchOption.AllDirectories);

            for (int i = 0; i < paths.Length; i++)
            {
                FileInfo info = new FileInfo(paths[i]);
                FileSignature signature = new FileSignature();
                signature.Length = info.Length;
                signature.Hash = ComputeSha256(info.FullName);
                files[MakeRelative(sessionsPath, paths[i])] = signature;
            }

            return files;
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

        private static string ResolveSessionsPath(string rootPath)
        {
            string full = Path.GetFullPath(rootPath);
            string sessions = Path.Combine(full, "sessions");

            if (Directory.Exists(sessions))
            {
                return sessions;
            }

            return full;
        }

        private static string MakeRelative(string rootPath, string path)
        {
            Uri rootUri = new Uri(AppendDirectorySeparator(Path.GetFullPath(rootPath)));
            Uri pathUri = new Uri(Path.GetFullPath(path));
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AppendDirectorySeparator(string path)
        {
            if (path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
            {
                return path;
            }

            return path + Path.DirectorySeparatorChar;
        }

        private static void ValidateOptions(StateForgeReplicaCatchUpOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            if (string.IsNullOrWhiteSpace(options.PrimaryRootPath))
            {
                throw new ArgumentException("PrimaryRootPath is required.", "options");
            }

            if (string.IsNullOrWhiteSpace(options.ReplicaRootPath))
            {
                throw new ArgumentException("ReplicaRootPath is required.", "options");
            }
        }

        private static string ResolveReplicaName(StateForgeReplicaCatchUpOptions options)
        {
            if (!string.IsNullOrWhiteSpace(options.ReplicaName))
            {
                return options.ReplicaName;
            }

            string fullPath = Path.GetFullPath(options.ReplicaRootPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string name = Path.GetFileName(fullPath);
            return string.IsNullOrWhiteSpace(name) ? "replica" : name;
        }

        private sealed class FileSignature
        {
            public long Length { get; set; }

            public string Hash { get; set; }
        }
    }
}
