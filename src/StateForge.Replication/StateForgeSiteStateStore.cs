using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace StateForge.Replication
{
    public static class StateForgeSiteStateStore
    {
        public const string FileName = "stateforge-site-state.json";

        public static StateForgeSiteState Read(string siteRootPath)
        {
            string path = GetPath(siteRootPath);
            if (!File.Exists(path))
            {
                return null;
            }

            string json = File.ReadAllText(path);
            string document = (json ?? string.Empty).Trim();
            if (document.Length == 0 || document[0] != '{' || document[document.Length - 1] != '}')
            {
                throw new InvalidDataException("Site state file is incomplete or invalid: " + path);
            }

            StateForgeSiteState state = new StateForgeSiteState();
            state.Version = ReadRequiredString(json, "version");
            state.SiteName = ReadRequiredString(json, "siteName");
            state.Region = ReadRequiredString(json, "region");
            state.Role = ReadRole(json);
            state.RootPath = ReadRequiredString(json, "rootPath");
            state.Enabled = ReadRequiredBoolean(json, "enabled");
            state.Healthy = ReadRequiredBoolean(json, "healthy");
            state.PromotionEligible = ReadRequiredBoolean(json, "promotionEligible");
            state.LastHeartbeatUtc = ReadRequiredDate(json, "lastHeartbeatUtc");
            state.LastRecoveryPointUtc = ReadRequiredDate(json, "lastRecoveryPointUtc");
            state.LastError = ReadOptionalString(json, "lastError");

            if (!string.Equals(state.Version, "1", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Unsupported site state version '" + state.Version + "'.");
            }

            if (!Path.IsPathRooted(state.RootPath))
            {
                throw new InvalidDataException("Site state root path must be absolute.");
            }

            return state;
        }

        public static void Write(string siteRootPath, StateForgeSiteState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException("state");
            }

            string root = Path.GetFullPath(siteRootPath);
            Directory.CreateDirectory(root);
            state.RootPath = root;
            string path = GetPath(root);
            using (StateForgeReplicaStateMutex.Acquire(path))
            {
                WriteAtomic(path, state);
            }
        }

        public static string GetPath(string siteRootPath)
        {
            if (string.IsNullOrWhiteSpace(siteRootPath))
            {
                throw new ArgumentException("Site root path is required.", "siteRootPath");
            }

            return Path.Combine(Path.GetFullPath(siteRootPath), FileName);
        }

        private static void WriteAtomic(string path, StateForgeSiteState state)
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

        private static string ToJson(StateForgeSiteState state)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"version\": \"" + Escape(state.Version) + "\",");
            builder.AppendLine("  \"siteName\": \"" + Escape(state.SiteName) + "\",");
            builder.AppendLine("  \"region\": \"" + Escape(state.Region) + "\",");
            builder.AppendLine("  \"role\": \"" + state.Role.ToString() + "\",");
            builder.AppendLine("  \"rootPath\": \"" + Escape(state.RootPath) + "\",");
            builder.AppendLine("  \"enabled\": " + Boolean(state.Enabled) + ",");
            builder.AppendLine("  \"healthy\": " + Boolean(state.Healthy) + ",");
            builder.AppendLine("  \"promotionEligible\": " + Boolean(state.PromotionEligible) + ",");
            builder.AppendLine("  \"lastHeartbeatUtc\": \"" + Date(state.LastHeartbeatUtc) + "\",");
            builder.AppendLine("  \"lastRecoveryPointUtc\": \"" + Date(state.LastRecoveryPointUtc) + "\",");
            builder.AppendLine("  \"lastError\": \"" + Escape(state.LastError) + "\"");
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static string Date(DateTimeOffset value)
        {
            return value.ToString("o", CultureInfo.InvariantCulture);
        }

        private static string Boolean(bool value)
        {
            return value ? "true" : "false";
        }

        private static StateForgeSiteRole ReadRole(string json)
        {
            string role = ReadRequiredString(json, "role");
            StateForgeSiteRole value;
            if (!Enum.TryParse(role, true, out value) || !Enum.IsDefined(typeof(StateForgeSiteRole), value))
            {
                throw new InvalidDataException("Site state property 'role' is invalid.");
            }

            return value;
        }

        private static DateTimeOffset ReadRequiredDate(string json, string name)
        {
            DateTimeOffset value;
            if (!DateTimeOffset.TryParse(
                ReadRequiredString(json, name),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out value))
            {
                throw new InvalidDataException("Site state property '" + name + "' is not a valid timestamp.");
            }

            return value;
        }

        private static bool ReadRequiredBoolean(string json, string name)
        {
            Match match = Regex.Match(
                json,
                "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*(?<value>true|false)",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                throw new InvalidDataException("Site state property '" + name + "' must be a boolean.");
            }

            return string.Equals(match.Groups["value"].Value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadRequiredString(string json, string name)
        {
            string value = ReadOptionalString(json, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException("Site state property '" + name + "' is required.");
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
