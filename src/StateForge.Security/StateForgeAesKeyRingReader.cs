using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace StateForge.Security
{
    public static class StateForgeAesKeyRingReader
    {
        public static StateForgeAesKeyRing Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required.", "path");
            }

            string json = File.ReadAllText(path);
            return FromJson(json);
        }

        public static StateForgeAesKeyRing FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON is required.", "json");
            }

            StateForgeAesKeyRing ring = new StateForgeAesKeyRing();
            ring.Version = MatchString(json, "version") ?? "1";
            ring.CurrentKeyId = MatchString(json, "currentKeyId");

            MatchCollection keyMatches = Regex.Matches(json, "\\{\\s*\\\"keyId\\\"\\s*:\\s*\\\"(?<keyId>[^\\\"]+)\\\"\\s*,\\s*\\\"keyBase64\\\"\\s*:\\s*\\\"(?<keyBase64>[^\\\"]+)\\\"\\s*,\\s*\\\"createdUtc\\\"\\s*:\\s*\\\"(?<createdUtc>[^\\\"]+)\\\"\\s*,\\s*\\\"notBeforeUtc\\\"\\s*:\\s*(?<notBefore>null|\\\"[^\\\"]+\\\")\\s*,\\s*\\\"retiredUtc\\\"\\s*:\\s*(?<retired>null|\\\"[^\\\"]+\\\")\\s*\\}", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            foreach (Match match in keyMatches)
            {
                StateForgeAesKeyInfo key = new StateForgeAesKeyInfo();
                key.KeyId = match.Groups["keyId"].Value;
                key.KeyBase64 = match.Groups["keyBase64"].Value;
                key.CreatedUtc = ParseDate(match.Groups["createdUtc"].Value, DateTimeOffset.UtcNow);
                key.NotBeforeUtc = ParseNullableDate(match.Groups["notBefore"].Value);
                key.RetiredUtc = ParseNullableDate(match.Groups["retired"].Value);
                ring.Keys.Add(key);
            }

            return ring;
        }

        private static string MatchString(string json, string name)
        {
            Match match = Regex.Match(json, "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*\\\"(?<value>[^\\\"]*)\\\"", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return null;
            }

            return match.Groups["value"].Value;
        }

        private static DateTimeOffset ParseDate(string value, DateTimeOffset fallback)
        {
            DateTimeOffset parsed;

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed))
            {
                return parsed;
            }

            return fallback;
        }

        private static DateTimeOffset? ParseNullableDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            value = value.Trim();

            if (value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal))
            {
                value = value.Substring(1, value.Length - 2);
            }

            DateTimeOffset parsed;

            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
