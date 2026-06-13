using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace StateForge.Replication
{
    public static class StateForgePrimaryLeaseStore
    {
        public const string FileName = "stateforge-primary-lease.json";

        public static StateForgePrimaryLease Read(string leaseRootPath)
        {
            string path = GetPath(leaseRootPath);
            if (!File.Exists(path))
            {
                return null;
            }

            string json = File.ReadAllText(path);
            string document = (json ?? string.Empty).Trim();
            if (document.Length == 0 || document[0] != '{' || document[document.Length - 1] != '}')
            {
                throw new InvalidDataException("Primary lease file is incomplete or invalid: " + path);
            }

            StateForgePrimaryLease lease = new StateForgePrimaryLease();
            lease.Version = ReadRequiredString(json, "version");
            lease.ClusterName = ReadRequiredString(json, "clusterName");
            lease.PrimaryName = ReadRequiredString(json, "primaryName");
            lease.LeaseId = ReadRequiredString(json, "leaseId");
            lease.Epoch = ReadRequiredLong(json, "epoch");
            lease.AcquiredUtc = ReadRequiredDate(json, "acquiredUtc");
            lease.RenewedUtc = ReadRequiredDate(json, "renewedUtc");
            lease.ExpiresUtc = ReadRequiredDate(json, "expiresUtc");

            if (!string.Equals(lease.Version, "1", StringComparison.Ordinal))
            {
                throw new InvalidDataException("Unsupported primary lease version '" + lease.Version + "'.");
            }

            if (lease.Epoch < 1 || lease.ExpiresUtc <= lease.AcquiredUtc)
            {
                throw new InvalidDataException("Primary lease contains an invalid epoch or expiration.");
            }

            return lease;
        }

        public static string GetPath(string leaseRootPath)
        {
            if (string.IsNullOrWhiteSpace(leaseRootPath))
            {
                throw new ArgumentException("Lease root path is required.", "leaseRootPath");
            }

            return Path.Combine(Path.GetFullPath(leaseRootPath), FileName);
        }

        internal static void WriteLocked(string leaseRootPath, StateForgePrimaryLease lease)
        {
            if (lease == null)
            {
                throw new ArgumentNullException("lease");
            }

            string root = Path.GetFullPath(leaseRootPath);
            Directory.CreateDirectory(root);
            string path = GetPath(root);
            string tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";

            try
            {
                File.WriteAllText(tempPath, ToJson(lease), new UTF8Encoding(false));
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

        private static string ToJson(StateForgePrimaryLease lease)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"version\": \"" + Escape(lease.Version) + "\",");
            builder.AppendLine("  \"clusterName\": \"" + Escape(lease.ClusterName) + "\",");
            builder.AppendLine("  \"primaryName\": \"" + Escape(lease.PrimaryName) + "\",");
            builder.AppendLine("  \"leaseId\": \"" + Escape(lease.LeaseId) + "\",");
            builder.AppendLine("  \"epoch\": " + lease.Epoch.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"acquiredUtc\": \"" + lease.AcquiredUtc.ToString("o", CultureInfo.InvariantCulture) + "\",");
            builder.AppendLine("  \"renewedUtc\": \"" + lease.RenewedUtc.ToString("o", CultureInfo.InvariantCulture) + "\",");
            builder.AppendLine("  \"expiresUtc\": \"" + lease.ExpiresUtc.ToString("o", CultureInfo.InvariantCulture) + "\"");
            builder.AppendLine("}");
            return builder.ToString();
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
                throw new InvalidDataException("Primary lease property '" + name + "' is not a valid timestamp.");
            }

            return value;
        }

        private static long ReadRequiredLong(string json, string name)
        {
            Match match = Regex.Match(
                json,
                "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*(?<value>-?[0-9]+)",
                RegexOptions.IgnoreCase);
            long value;
            if (!match.Success ||
                !long.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            {
                throw new InvalidDataException("Primary lease property '" + name + "' must be an integer.");
            }

            return value;
        }

        private static string ReadRequiredString(string json, string name)
        {
            Match match = Regex.Match(
                json,
                "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"",
                RegexOptions.IgnoreCase);
            string value = match.Success ? Unescape(match.Groups["value"].Value) : string.Empty;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException("Primary lease property '" + name + "' is required.");
            }

            return value;
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
