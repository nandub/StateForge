using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace StateForge.Replication
{
    public static class StateForgeWitnessStateStore
    {
        public const string FileName = "stateforge-witness-state.json";

        public static StateForgeWitnessState Read(string witnessRootPath)
        {
            string path = GetPath(witnessRootPath);
            if (!File.Exists(path))
            {
                return null;
            }

            string json = File.ReadAllText(path);
            string document = (json ?? string.Empty).Trim();
            if (document.Length == 0 || document[0] != '{' || document[document.Length - 1] != '}')
            {
                throw new InvalidDataException("Witness state file is incomplete or invalid: " + path);
            }

            StateForgeWitnessState state = new StateForgeWitnessState();
            state.Version = ReadRequiredString(json, "version");
            state.WitnessName = ReadRequiredString(json, "witnessName");
            state.WitnessRootPath = ReadRequiredString(json, "witnessRootPath");
            state.LastHeartbeatUtc = ReadRequiredDate(json, "lastHeartbeatUtc");
            state.CandidateName = ReadOptionalString(json, "candidateName");
            state.VoteGranted = ReadRequiredBoolean(json, "voteGranted");
            state.LastError = ReadOptionalString(json, "lastError");

            if (!string.Equals(state.Version, "1", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Unsupported witness state version '" + state.Version + "'.");
            }

            return state;
        }

        public static void Write(string witnessRootPath, StateForgeWitnessState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            string fullRoot = Path.GetFullPath(witnessRootPath);
            Directory.CreateDirectory(fullRoot);
            state.WitnessName = string.IsNullOrWhiteSpace(state.WitnessName)
                ? "witness"
                : state.WitnessName.Trim();
            state.WitnessRootPath = fullRoot;

            string path = GetPath(fullRoot);
            using (StateForgeReplicaStateMutex.Acquire(path))
            {
                WriteAtomic(path, state);
            }
        }

        public static string GetPath(string witnessRootPath)
        {
            if (string.IsNullOrWhiteSpace(witnessRootPath))
            {
                throw new ArgumentException("Witness root path is required.", "witnessRootPath");
            }

            return Path.Combine(Path.GetFullPath(witnessRootPath), FileName);
        }

        private static void WriteAtomic(string path, StateForgeWitnessState state)
        {
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

        private static string ToJson(StateForgeWitnessState state)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"version\": \"" + Escape(state.Version) + "\",");
            builder.AppendLine("  \"witnessName\": \"" + Escape(state.WitnessName) + "\",");
            builder.AppendLine("  \"witnessRootPath\": \"" + Escape(state.WitnessRootPath) + "\",");
            builder.AppendLine("  \"lastHeartbeatUtc\": \"" +
                state.LastHeartbeatUtc.ToString("o", CultureInfo.InvariantCulture) + "\",");
            builder.AppendLine("  \"candidateName\": \"" + Escape(state.CandidateName) + "\",");
            builder.AppendLine("  \"voteGranted\": " + (state.VoteGranted ? "true" : "false") + ",");
            builder.AppendLine("  \"lastError\": \"" + Escape(state.LastError) + "\"");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static DateTimeOffset ReadRequiredDate(string json, string name)
        {
            string value = ReadRequiredString(json, name);
            DateTimeOffset parsed;
            if (!DateTimeOffset.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out parsed))
            {
                throw new InvalidDataException("Witness state property '" + name + "' is not a valid timestamp.");
            }

            return parsed;
        }

        private static bool ReadRequiredBoolean(string json, string name)
        {
            Match match = Regex.Match(
                json,
                "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*(?<value>true|false)",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                throw new InvalidDataException("Witness state property '" + name + "' must be a boolean.");
            }

            return string.Equals(match.Groups["value"].Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadRequiredString(string json, string name)
        {
            string value = ReadOptionalString(json, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException("Witness state property '" + name + "' is required.");
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
