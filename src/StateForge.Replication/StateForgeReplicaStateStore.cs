using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace StateForge.Replication
{
    public static class StateForgeReplicaStateStore
    {
        public const string FileName = "stateforge-replica-state.json";

        public static StateForgeReplicaSyncState Read(string replicaRootPath)
        {
            string path = GetPath(replicaRootPath);
            if (!File.Exists(path)) { return null; }

            string json = File.ReadAllText(path);
            ValidateDocument(json, path);

            StateForgeReplicaSyncState state = new StateForgeReplicaSyncState();
            state.Version = ReadRequiredString(json, "version");
            state.ReplicaName = ReadRequiredString(json, "replicaName");
            state.ReplicaRootPath = ReadRequiredString(json, "replicaRootPath");
            state.LastAttemptUtc = ReadDate(json, "lastAttemptUtc");
            state.LastSuccessfulSyncUtc = ReadDate(json, "lastSuccessfulSyncUtc");
            state.CatchUpOperations = ReadRequiredLong(json, "catchUpOperations");
            state.FailedSyncs = ReadRequiredLong(json, "failedSyncs");
            state.LastError = ReadOptionalString(json, "lastError");

            if (!string.Equals(state.Version, "1", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Unsupported replica sync state version '" + state.Version + "'.");
            }

            return state;
        }

        public static void RecordReplication(
            string replicaRootPath,
            string replicaName,
            bool success,
            string error,
            DateTimeOffset attemptedUtc)
        {
            Update(replicaRootPath, replicaName, success, false, error, attemptedUtc);
        }

        public static void RecordCatchUp(
            string replicaRootPath,
            string replicaName,
            bool success,
            string error,
            DateTimeOffset attemptedUtc)
        {
            Update(replicaRootPath, replicaName, success, true, error, attemptedUtc);
        }

        public static string GetPath(string replicaRootPath)
        {
            if (string.IsNullOrWhiteSpace(replicaRootPath))
            {
                throw new ArgumentException("Replica root path is required.", "replicaRootPath");
            }

            return Path.Combine(Path.GetFullPath(replicaRootPath), FileName);
        }

        private static void Update(
            string replicaRootPath,
            string replicaName,
            bool success,
            bool catchUp,
            string error,
            DateTimeOffset attemptedUtc)
        {
            string fullRoot = Path.GetFullPath(replicaRootPath);
            Directory.CreateDirectory(fullRoot);

            using (StateForgeReplicaStateMutex.Acquire(GetPath(fullRoot)))
            {
                StateForgeReplicaSyncState state = Read(fullRoot) ?? new StateForgeReplicaSyncState();
                state.ReplicaName = string.IsNullOrWhiteSpace(replicaName) ? "replica" : replicaName;
                state.ReplicaRootPath = fullRoot;
                state.LastAttemptUtc = attemptedUtc;

                if (catchUp)
                {
                    state.CatchUpOperations++;
                }

                if (success)
                {
                    state.LastSuccessfulSyncUtc = attemptedUtc;
                    state.LastError = string.Empty;
                }
                else
                {
                    state.FailedSyncs++;
                    state.LastError = error ?? string.Empty;
                }

                WriteAtomic(fullRoot, state);
            }
        }

        private static void WriteAtomic(string replicaRootPath, StateForgeReplicaSyncState state)
        {
            string path = GetPath(replicaRootPath);
            string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                File.WriteAllText(tempPath, ToJson(state), new UTF8Encoding(false));

                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, null, true);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }

        private static string ToJson(StateForgeReplicaSyncState state)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"version\": \"" + Escape(state.Version) + "\",");
            builder.AppendLine("  \"replicaName\": \"" + Escape(state.ReplicaName) + "\",");
            builder.AppendLine("  \"replicaRootPath\": \"" + Escape(state.ReplicaRootPath) + "\",");
            builder.AppendLine("  \"lastAttemptUtc\": \"" + FormatDate(state.LastAttemptUtc) + "\",");
            builder.AppendLine("  \"lastSuccessfulSyncUtc\": \"" + FormatDate(state.LastSuccessfulSyncUtc) + "\",");
            builder.AppendLine("  \"catchUpOperations\": " + state.CatchUpOperations.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"failedSyncs\": " + state.FailedSyncs.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"lastError\": \"" + Escape(state.LastError) + "\"");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string FormatDate(DateTimeOffset? value)
        {
            return value.HasValue ? value.Value.ToString("o", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static DateTimeOffset? ReadDate(string json, string name)
        {
            string value = ReadOptionalString(json, name);
            if (value.Length == 0)
            {
                return null;
            }

            DateTimeOffset parsed;
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
            {
                throw new InvalidDataException(
                    "Replica sync state property '" + name + "' is not a valid timestamp.");
            }

            return parsed;
        }

        private static string ReadRequiredString(string json, string name)
        {
            string value = ReadOptionalString(json, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException(
                    "Replica sync state property '" + name + "' is required.");
            }

            return value;
        }

        private static string ReadOptionalString(string json, string name)
        {
            Match match = Regex.Match(
                json,
                "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"",
                RegexOptions.IgnoreCase);
            return match.Success ? Unescape(match.Groups["value"].Value) : string.Empty;
        }

        private static long ReadRequiredLong(string json, string name)
        {
            Match match = Regex.Match(
                json,
                "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*(?<value>\\d+)",
                RegexOptions.IgnoreCase);
            long value;
            if (!match.Success ||
                !long.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                throw new InvalidDataException(
                    "Replica sync state property '" + name + "' must be a non-negative integer.");
            }

            return value;
        }

        private static void ValidateDocument(string json, string path)
        {
            string value = (json ?? string.Empty).Trim();
            if (value.Length == 0 || value[0] != '{' || value[value.Length - 1] != '}')
            {
                throw new InvalidDataException(
                    "Replica sync state file is incomplete or invalid: " + path);
            }
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }

        private static string Unescape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }
    }
}
