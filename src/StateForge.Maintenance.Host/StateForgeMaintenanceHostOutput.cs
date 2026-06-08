using System;
using System.IO;
using System.Text;

namespace StateForge.Maintenance.Host
{
    internal static class StateForgeMaintenanceHostOutput
    {
        public static void Write(StateForgeMaintenanceHostResult result, bool json, string logPath)
        {
            string text = json ? ToJson(result) : ToText(result);
            Console.WriteLine(text);

            if (!string.IsNullOrWhiteSpace(logPath))
            {
                string directory = Path.GetDirectoryName(Path.GetFullPath(logPath));

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                RotateLogIfNeeded(logPath, 50, 10);
                File.AppendAllText(logPath, text + Environment.NewLine, Encoding.UTF8);
            }
        }

        private static string ToText(StateForgeMaintenanceHostResult result)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("StateForge Maintenance Host");
            builder.AppendLine("---------------------------");
            builder.AppendLine("Success=" + result.Success);
            builder.AppendLine("RootPath=" + result.RootPath);
            builder.AppendLine("StartedUtc=" + result.StartedUtc);
            builder.AppendLine("CompletedUtc=" + result.CompletedUtc);
            builder.AppendLine("CleanupRan=" + result.CleanupRan);
            builder.AppendLine("CleanupExpiredDeleted=" + result.CleanupExpiredDeleted);
            builder.AppendLine("CleanupInvalidQuarantined=" + result.CleanupInvalidQuarantined);
            builder.AppendLine("CleanupFailed=" + result.CleanupFailed);
            builder.AppendLine("HealthRan=" + result.HealthRan);
            builder.AppendLine("Healthy=" + result.Healthy);
            builder.AppendLine("StatsRan=" + result.StatsRan);
            builder.AppendLine("TotalSessions=" + result.TotalSessions);
            builder.AppendLine("MigrationRan=" + result.MigrationRan);
            builder.AppendLine("MigrationFilesScanned=" + result.MigrationFilesScanned);
            builder.AppendLine("MigrationMigratedFiles=" + result.MigrationMigratedFiles);
            builder.AppendLine("MigrationFailedFiles=" + result.MigrationFailedFiles);

            for (int i = 0; i < result.Errors.Count; i++)
            {
                builder.AppendLine("Error=" + result.Errors[i]);
            }

            return builder.ToString();
        }

        private static string ToJson(StateForgeMaintenanceHostResult result)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("{");
            Append(builder, "success", result.Success, true);
            Append(builder, "rootPath", result.RootPath, true);
            Append(builder, "startedUtc", result.StartedUtc, true);
            Append(builder, "completedUtc", result.CompletedUtc, true);
            Append(builder, "cleanupRan", result.CleanupRan, true);
            Append(builder, "cleanupExpiredDeleted", result.CleanupExpiredDeleted, true);
            Append(builder, "cleanupInvalidQuarantined", result.CleanupInvalidQuarantined, true);
            Append(builder, "cleanupFailed", result.CleanupFailed, true);
            Append(builder, "healthRan", result.HealthRan, true);
            Append(builder, "healthy", result.Healthy, true);
            Append(builder, "statsRan", result.StatsRan, true);
            Append(builder, "totalSessions", result.TotalSessions, true);
            Append(builder, "migrationRan", result.MigrationRan, true);
            Append(builder, "migrationFilesScanned", result.MigrationFilesScanned, true);
            Append(builder, "migrationMigratedFiles", result.MigrationMigratedFiles, true);
            Append(builder, "migrationFailedFiles", result.MigrationFailedFiles, true);
            builder.Append("\"errors\":[");
            for (int i = 0; i < result.Errors.Count; i++)
            {
                if (i > 0) builder.Append(",");
                builder.Append("\"").Append(Escape(result.Errors[i])).Append("\"");
            }
            builder.Append("]}");
            return builder.ToString();
        }

        private static void Append(StringBuilder builder, string name, string value, bool comma)
        {
            builder.Append("\"").Append(name).Append("\":\"").Append(Escape(value)).Append("\"");
            if (comma) builder.Append(",");
        }

        private static void Append(StringBuilder builder, string name, bool value, bool comma)
        {
            builder.Append("\"").Append(name).Append("\":").Append(value ? "true" : "false");
            if (comma) builder.Append(",");
        }

        private static void Append(StringBuilder builder, string name, int value, bool comma)
        {
            builder.Append("\"").Append(name).Append("\":").Append(value);
            if (comma) builder.Append(",");
        }


        private static void RotateLogIfNeeded(string logPath, int maxLogSizeMb, int maxLogFiles)
        {
            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            {
                return;
            }

            FileInfo info = new FileInfo(logPath);
            long maxBytes = (long)maxLogSizeMb * 1024L * 1024L;

            if (info.Length < maxBytes)
            {
                return;
            }

            if (maxLogFiles <= 0)
            {
                File.Delete(logPath);
                return;
            }

            string oldest = logPath + "." + maxLogFiles.ToString();

            if (File.Exists(oldest))
            {
                File.Delete(oldest);
            }

            for (int i = maxLogFiles - 1; i >= 1; i--)
            {
                string source = logPath + "." + i.ToString();
                string destination = logPath + "." + (i + 1).ToString();

                if (File.Exists(source))
                {
                    File.Move(source, destination);
                }
            }

            File.Move(logPath, logPath + ".1");
        }

        private static string Escape(string value)
        {
            if (value == null) return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
