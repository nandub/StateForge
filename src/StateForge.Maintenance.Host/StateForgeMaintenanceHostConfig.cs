using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace StateForge.Maintenance.Host
{
    internal sealed class StateForgeMaintenanceHostConfig
    {
        public string RootPath { get; set; }
        public int IntervalSeconds { get; set; }
        public bool CleanupEnabled { get; set; }
        public bool HealthEnabled { get; set; }
        public bool StatsEnabled { get; set; }
        public bool Stfg2MigrationEnabled { get; set; }
        public bool Stfg2MigrationDryRun { get; set; }
        public bool Stfg2MigrationApply { get; set; }
        public string Stfg2MigrationKeyId { get; set; }
        public string Stfg2MigrationPattern { get; set; }
        public string LogPath { get; set; }
        public bool Json { get; set; }
        public int MaxLogSizeMb { get; set; }
        public int MaxLogFiles { get; set; }

        public StateForgeMaintenanceHostConfig()
        {
            IntervalSeconds = 900;
            CleanupEnabled = true;
            HealthEnabled = true;
            StatsEnabled = true;
            Stfg2MigrationEnabled = false;
            Stfg2MigrationDryRun = true;
            Stfg2MigrationApply = false;
            Stfg2MigrationPattern = "*.stfg";
            Json = false;
            MaxLogSizeMb = 50;
            MaxLogFiles = 10;
        }

        public static StateForgeMaintenanceHostConfig Load(string path)
        {
            StateForgeMaintenanceHostConfig config = new StateForgeMaintenanceHostConfig();

            if (string.IsNullOrWhiteSpace(path))
            {
                return config;
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Config file was not found.", path);
            }

            string json = File.ReadAllText(path);
            config.RootPath = ReadString(json, "rootPath", config.RootPath);
            config.IntervalSeconds = ReadInt(json, "intervalSeconds", config.IntervalSeconds);
            config.CleanupEnabled = ReadBool(json, "cleanupEnabled", config.CleanupEnabled);
            config.HealthEnabled = ReadBool(json, "healthEnabled", config.HealthEnabled);
            config.StatsEnabled = ReadBool(json, "statsEnabled", config.StatsEnabled);
            config.Stfg2MigrationEnabled = ReadBool(json, "stfg2MigrationEnabled", config.Stfg2MigrationEnabled);
            config.Stfg2MigrationDryRun = ReadBool(json, "stfg2MigrationDryRun", config.Stfg2MigrationDryRun);
            config.Stfg2MigrationApply = ReadBool(json, "stfg2MigrationApply", config.Stfg2MigrationApply);
            config.Stfg2MigrationKeyId = ReadString(json, "stfg2MigrationKeyId", config.Stfg2MigrationKeyId);
            config.Stfg2MigrationPattern = ReadString(json, "stfg2MigrationPattern", config.Stfg2MigrationPattern);
            config.LogPath = ReadString(json, "logPath", config.LogPath);
            config.Json = ReadBool(json, "json", config.Json);
            config.MaxLogSizeMb = ReadInt(json, "maxLogSizeMb", config.MaxLogSizeMb);
            config.MaxLogFiles = ReadInt(json, "maxLogFiles", config.MaxLogFiles);
            return config;
        }

        private static string ReadString(string json, string name, string fallback)
        {
            Match match = Regex.Match(json, "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*\\\"(?<value>[^\\\"]*)\\\"", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["value"].Value : fallback;
        }

        private static int ReadInt(string json, string name, int fallback)
        {
            Match match = Regex.Match(json, "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*(?<value>\\d+)", RegexOptions.IgnoreCase);
            int value;
            return match.Success && int.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : fallback;
        }

        private static bool ReadBool(string json, string name, bool fallback)
        {
            Match match = Regex.Match(json, "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*(?<value>true|false)", RegexOptions.IgnoreCase);
            bool value;
            return match.Success && bool.TryParse(match.Groups["value"].Value, out value) ? value : fallback;
        }
    }
}
