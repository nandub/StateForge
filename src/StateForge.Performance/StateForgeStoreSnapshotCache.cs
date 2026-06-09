using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using StateForge.FileStore;

namespace StateForge.Performance
{
    public static class StateForgeStoreSnapshotCache
    {
        public static StateForgeStoreSnapshot Capture(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path is required.", "rootPath");
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = rootPath;

            StateForgeFileStore store = new StateForgeFileStore(options);
            object stats = store.GetStats();

            stopwatch.Stop();

            StateForgeStoreSnapshot snapshot = new StateForgeStoreSnapshot();
            snapshot.RootPath = rootPath;
            snapshot.CapturedUtc = DateTimeOffset.UtcNow.ToString("o");
            snapshot.TotalSessions = ReadInt(stats, "TotalSessions");
            snapshot.ExpiredSessions = ReadInt(stats, "ExpiredSessions");
            snapshot.LockedSessions = ReadInt(stats, "LockedSessions");
            snapshot.CompressedSessions = ReadInt(stats, "CompressedSessions");
            snapshot.EncryptedSessions = ReadInt(stats, "EncryptedSessions");
            snapshot.AesEncryptedSessions = ReadInt(stats, "AesEncryptedSessions");
            snapshot.TotalPayloadBytes = ReadLong(stats, "TotalPayloadBytes");
            snapshot.AveragePayloadBytes = ReadLong(stats, "AveragePayloadBytes");
            snapshot.CaptureElapsedMs = stopwatch.ElapsedMilliseconds;

            return snapshot;
        }

        public static StateForgeStoreSnapshot CaptureAndWrite(string rootPath, string snapshotPath)
        {
            StateForgeStoreSnapshot snapshot = Capture(rootPath);
            Write(snapshotPath, snapshot);
            return snapshot;
        }

        public static void Write(string snapshotPath, StateForgeStoreSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(snapshotPath))
            {
                throw new ArgumentException("Snapshot path is required.", "snapshotPath");
            }

            string fullPath = Path.GetFullPath(snapshotPath);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, ToJson(snapshot), Encoding.UTF8);
        }

        public static StateForgeStoreSnapshot Read(string snapshotPath)
        {
            if (!File.Exists(snapshotPath))
            {
                throw new FileNotFoundException("Snapshot file was not found.", snapshotPath);
            }

            string json = File.ReadAllText(snapshotPath);
            StateForgeStoreSnapshot snapshot = new StateForgeStoreSnapshot();
            snapshot.RootPath = ReadString(json, "rootPath");
            snapshot.CapturedUtc = ReadString(json, "capturedUtc");
            snapshot.TotalSessions = ReadInt(json, "totalSessions");
            snapshot.ExpiredSessions = ReadInt(json, "expiredSessions");
            snapshot.LockedSessions = ReadInt(json, "lockedSessions");
            snapshot.CompressedSessions = ReadInt(json, "compressedSessions");
            snapshot.EncryptedSessions = ReadInt(json, "encryptedSessions");
            snapshot.AesEncryptedSessions = ReadInt(json, "aesEncryptedSessions");
            snapshot.TotalPayloadBytes = ReadLong(json, "totalPayloadBytes");
            snapshot.AveragePayloadBytes = ReadLong(json, "averagePayloadBytes");
            snapshot.CaptureElapsedMs = ReadLong(json, "captureElapsedMs");
            return snapshot;
        }

        public static string ToJson(StateForgeStoreSnapshot snapshot)
        {
            if (snapshot == null)
            {
                snapshot = new StateForgeStoreSnapshot();
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"version\": \"0.18.2\",");
            builder.AppendLine("  \"rootPath\": \"" + Escape(snapshot.RootPath) + "\",");
            builder.AppendLine("  \"capturedUtc\": \"" + Escape(snapshot.CapturedUtc) + "\",");
            builder.AppendLine("  \"totalSessions\": " + snapshot.TotalSessions.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"expiredSessions\": " + snapshot.ExpiredSessions.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"lockedSessions\": " + snapshot.LockedSessions.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"compressedSessions\": " + snapshot.CompressedSessions.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"encryptedSessions\": " + snapshot.EncryptedSessions.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"aesEncryptedSessions\": " + snapshot.AesEncryptedSessions.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"totalPayloadBytes\": " + snapshot.TotalPayloadBytes.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"averagePayloadBytes\": " + snapshot.AveragePayloadBytes.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"captureElapsedMs\": " + snapshot.CaptureElapsedMs.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("}");
            return builder.ToString();
        }

        private static int ReadInt(object instance, string propertyName)
        {
            return Convert.ToInt32(ReadLong(instance, propertyName));
        }

        private static long ReadLong(object instance, string propertyName)
        {
            if (instance == null)
            {
                return 0;
            }

            object property = instance.GetType().GetProperty(propertyName).GetValue(instance, null);
            return property == null ? 0 : Convert.ToInt64(property);
        }

        private static string ReadString(string json, string name)
        {
            Match match = Regex.Match(json, "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*\\\"(?<value>[^\\\"]*)\\\"", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["value"].Value : string.Empty;
        }

        private static int ReadInt(string json, string name)
        {
            return Convert.ToInt32(ReadLong(json, name));
        }

        private static long ReadLong(string json, string name)
        {
            Match match = Regex.Match(json, "\\\"" + Regex.Escape(name) + "\\\"\\s*:\\s*(?<value>-?\\d+)", RegexOptions.IgnoreCase);
            long value;
            return match.Success && long.TryParse(match.Groups["value"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : 0;
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
