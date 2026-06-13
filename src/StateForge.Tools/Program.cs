using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using StateForge.Core;
using StateForge.FileStore;
using StateForge.Telemetry;
using StateForge.Security;
using StateForge.Format;
using StateForge.Prometheus;
using StateForge.Performance;
using StateForge.Replication;

namespace StateForge.Tools
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Usage();
                return 2;
            }

            string command = args[0];




            if (EqualsIgnoreCase(command, "snapshot"))
            {
                string snapshotRootPath = ReadOption(args, "--root");
                string snapshotPath = ReadOption(args, "--snapshot");

                if (string.IsNullOrWhiteSpace(snapshotRootPath))
                {
                    Console.Error.WriteLine("Missing required --root option.");
                    return 2;
                }

                if (string.IsNullOrWhiteSpace(snapshotPath))
                {
                    snapshotPath = "stateforge-store-snapshot.json";
                }

                StateForgeStoreSnapshot snapshot = StateForgeStoreSnapshotCache.CaptureAndWrite(snapshotRootPath, snapshotPath);
                Console.WriteLine(StateForgeStoreSnapshotCache.ToJson(snapshot));
                return 0;
            }

            if (EqualsIgnoreCase(command, "prometheus-snapshot"))
            {
                string snapshotPath = ReadOption(args, "--snapshot");

                if (string.IsNullOrWhiteSpace(snapshotPath))
                {
                    Console.Error.WriteLine("Missing required --snapshot option.");
                    return 2;
                }

                Console.Write(StateForgeSnapshotPrometheusCollector.CollectTextFromSnapshotFile(snapshotPath));
                return 0;
            }

            if (EqualsIgnoreCase(command, "dashboard"))
            {
                string dashboardRootPath = ReadOption(args, "--root");
                string dashboardReplicaConfiguration = ReadOption(args, "--replicas");
                string dashboardStaleSecondsValue = ReadOption(args, "--replica-stale-seconds");

                if (string.IsNullOrWhiteSpace(dashboardRootPath))
                {
                    Console.Error.WriteLine("Missing required --root option.");
                    return 2;
                }

                int dashboardStaleSeconds = 300;
                if (!string.IsNullOrWhiteSpace(dashboardStaleSecondsValue) &&
                    (!int.TryParse(dashboardStaleSecondsValue, out dashboardStaleSeconds) ||
                    dashboardStaleSeconds < 0))
                {
                    Console.Error.WriteLine("--replica-stale-seconds must be a non-negative integer.");
                    return 2;
                }

                StateForgeFileStoreOptions sfDashboardOptions = new StateForgeFileStoreOptions();
                sfDashboardOptions.RootPath = dashboardRootPath;
                StateForgeFileStore sfDashboardStore = new StateForgeFileStore(sfDashboardOptions);

                var sfDashboardStats = sfDashboardStore.GetStats();
                StateForgeHealthResult sfDashboardHealth = sfDashboardStore.CheckHealth();
                StateForgePrometheusSnapshot sfDashboardMetrics = StateForgePrometheusCollector.Collect(dashboardRootPath);
                List<StateForgeReplicaNode> sfDashboardReplicas =
                    StateForgeReplicaConfiguration.Parse(dashboardReplicaConfiguration);
                StateForgeReplicaMonitorSnapshot sfDashboardReplicaSnapshot =
                    StateForgeReplicaMonitor.Capture(
                        sfDashboardReplicas,
                        TimeSpan.FromSeconds(dashboardStaleSeconds));
                bool sfDashboardReplicasHealthy = true;

                for (int i = 0; i < sfDashboardReplicaSnapshot.Replicas.Count; i++)
                {
                    if (!sfDashboardReplicaSnapshot.Replicas[i].Healthy)
                    {
                        sfDashboardReplicasHealthy = false;
                    }
                }

                Console.WriteLine("StateForge Dashboard");
                Console.WriteLine("--------------------");
                Console.WriteLine();
                Console.WriteLine("Sessions");
                Console.WriteLine("  Active     : {0}", sfDashboardStats.TotalSessions);
                Console.WriteLine("  Expired    : {0}", sfDashboardStats.ExpiredSessions);
                Console.WriteLine("  Locked     : {0}", sfDashboardStats.LockedSessions);
                Console.WriteLine();
                Console.WriteLine("Storage");
                Console.WriteLine("  Compressed : {0}", sfDashboardStats.CompressedSessions);
                Console.WriteLine("  Encrypted  : {0}", sfDashboardStats.EncryptedSessions);
                Console.WriteLine("  AES        : {0}", sfDashboardStats.AesEncryptedSessions);
                Console.WriteLine("  Payload    : {0}", sfDashboardStats.TotalPayloadBytes);
                Console.WriteLine();
                Console.WriteLine("Operations");
                Console.WriteLine("  Reads      : {0}", sfDashboardMetrics.Reads);
                Console.WriteLine("  Writes     : {0}", sfDashboardMetrics.Writes);
                Console.WriteLine("  Deletes    : {0}", sfDashboardMetrics.Deletes);
                Console.WriteLine();
                Console.WriteLine("Maintenance");
                Console.WriteLine("  Cleanups   : {0}", sfDashboardMetrics.Cleanups);
                Console.WriteLine("  Quarantine : {0}", sfDashboardMetrics.Quarantines);
                Console.WriteLine("  Corruptions: {0}", sfDashboardMetrics.Corruptions);
                Console.WriteLine();
                Console.WriteLine("Health");
                Console.WriteLine("  Status     : {0}", sfDashboardHealth.Healthy ? "HEALTHY" : "UNHEALTHY");

                foreach (string error in sfDashboardHealth.Errors)
                {
                    Console.WriteLine("  Error      : {0}", error);
                }

                if (sfDashboardReplicaSnapshot.Replicas.Count > 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Replicas");

                    foreach (StateForgeReplicaMonitorEntry replica in sfDashboardReplicaSnapshot.Replicas)
                    {
                        string status = replica.Healthy ? "HEALTHY" : (replica.Stale ? "STALE" : "UNHEALTHY");
                        Console.WriteLine("  {0}", replica.ReplicaName);
                        Console.WriteLine("    Status       : {0}", status);
                        Console.WriteLine("    Root         : {0}", replica.ReplicaRootPath);
                        Console.WriteLine("    Lag Seconds  : {0:0.###}", replica.LagSeconds);
                        Console.WriteLine(
                            "    Last Sync UTC: {0}",
                            replica.LastSuccessfulSyncUtc.HasValue
                                ? replica.LastSuccessfulSyncUtc.Value.ToString("o")
                                : "never");
                        Console.WriteLine("    Catch-ups    : {0}", replica.CatchUpOperations);
                        Console.WriteLine("    Failed Syncs : {0}", replica.FailedSyncs);

                        if (!string.IsNullOrWhiteSpace(replica.LastError))
                        {
                            Console.WriteLine("    Last Error   : {0}", replica.LastError);
                        }
                    }
                }

                return sfDashboardHealth.Healthy && sfDashboardReplicasHealthy ? 0 : 1;
            }

            if (EqualsIgnoreCase(command, "prometheus"))
            {
                string prometheusRootPath = ReadOption(args, "--root");
                Console.Write(StateForgePrometheusCollector.CollectText(prometheusRootPath));
                return 0;
            }

            if (EqualsIgnoreCase(command, "keyring-create"))
            {
                string outFile = ReadOption(args, "--out");
                string keyId = ReadOption(args, "--key-id");

                if (string.IsNullOrWhiteSpace(outFile))
                {
                    Console.Error.WriteLine("Missing required --out option.");
                    return 2;
                }

                StateForgeAesKeyRing ring = StateForgeAesKeyRingManager.CreateNew(keyId);
                StateForgeAesKeyRingManager.Save(outFile, ring);

                Console.WriteLine("Created key ring: {0}", outFile);
                Console.WriteLine("CurrentKeyId={0}", ring.CurrentKeyId);
                return 0;
            }

            if (EqualsIgnoreCase(command, "keyring-generate-key"))
            {
                string keyId = ReadOption(args, "--key-id");
                StateForgeAesKeyInfo key = StateForgeAesKeyRingManager.CreateKey(keyId);

                if (EqualsIgnoreCase(ReadOption(args, "--format"), "json"))
                {
                    StateForgeAesKeyRing single = new StateForgeAesKeyRing();
                    single.CurrentKeyId = key.KeyId;
                    single.Keys.Add(key);
                    Console.WriteLine(StateForgeAesKeyRingJson.ToJson(single));
                }
                else
                {
                    Console.WriteLine("KeyId={0}", key.KeyId);
                    Console.WriteLine("KeyBase64={0}", key.KeyBase64);
                }

                return 0;
            }






            if (EqualsIgnoreCase(command, "stfg2-migrate-store"))
            {
                string rootPath = ReadOption(args, "--root");
                string keyId = ReadOption(args, "--key-id");
                string searchPattern = ReadOption(args, "--pattern");
                bool dryRun = HasSwitch(args, "--dry-run");
                bool apply = HasSwitch(args, "--apply");

                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    Console.Error.WriteLine("Missing required --root option.");
                    return 2;
                }

                if (!dryRun && !apply)
                {
                    Console.Error.WriteLine("Specify either --dry-run or --apply.");
                    return 2;
                }

                StateForgeStfg2StoreMigrationResult result = StateForgeStfg2StoreMigrator.MigrateStore(
                    rootPath,
                    keyId,
                    dryRun,
                    apply,
                    searchPattern);

                Console.WriteLine("RootPath={0}", result.RootPath);
                Console.WriteLine("DryRun={0}", result.DryRun);
                Console.WriteLine("Applied={0}", result.Applied);
                Console.WriteLine("FilesScanned={0}", result.FilesScanned);
                Console.WriteLine("LegacyFilesFound={0}", result.LegacyFilesFound);
                Console.WriteLine("Stfg2FilesSkipped={0}", result.Stfg2FilesSkipped);
                Console.WriteLine("MigratedFiles={0}", result.MigratedFiles);
                Console.WriteLine("FailedFiles={0}", result.FailedFiles);

                foreach (string error in result.Errors)
                {
                    Console.WriteLine("Error={0}", error);
                }

                return result.FailedFiles == 0 ? 0 : 1;
            }

            if (EqualsIgnoreCase(command, "stfg2-migrate"))
            {
                string source = ReadOption(args, "--source");
                string destination = ReadOption(args, "--destination");
                string keyId = ReadOption(args, "--key-id");
                bool overwrite = HasSwitch(args, "--overwrite");

                if (string.IsNullOrWhiteSpace(source))
                {
                    Console.Error.WriteLine("Missing required --source option.");
                    return 2;
                }

                if (string.IsNullOrWhiteSpace(destination))
                {
                    Console.Error.WriteLine("Missing required --destination option.");
                    return 2;
                }

                StateForgeStfg2MigrationResult result = StateForgeStfg2Migrator.MigrateFile(
                    source,
                    destination,
                    keyId,
                    overwrite);

                Console.WriteLine("SourcePath={0}", result.SourcePath);
                Console.WriteLine("DestinationPath={0}", result.DestinationPath);
                Console.WriteLine("SourceWasStfg2={0}", result.SourceWasStfg2);
                Console.WriteLine("Migrated={0}", result.Migrated);
                Console.WriteLine("KeyId={0}", result.KeyId);
                Console.WriteLine("OriginalLength={0}", result.OriginalLength);
                Console.WriteLine("NewLength={0}", result.NewLength);
                return 0;
            }

            if (EqualsIgnoreCase(command, "stfg2-create"))
            {
                string outFile = ReadOption(args, "--out");
                string keyId = ReadOption(args, "--key-id");
                string text = ReadOption(args, "--text");

                if (string.IsNullOrWhiteSpace(outFile))
                {
                    Console.Error.WriteLine("Missing required --out option.");
                    return 2;
                }

                if (text == null)
                {
                    text = string.Empty;
                }

                byte[] payload = System.Text.Encoding.UTF8.GetBytes(text);
                byte[] bytes = StateForgeStfg2.Write(
                    payload,
                    StateForgeFormatFlags.Compressed | StateForgeFormatFlags.Encrypted | StateForgeFormatFlags.Aes,
                    keyId);

                File.WriteAllBytes(outFile, bytes);

                Console.WriteLine("Created={0}", outFile);
                Console.WriteLine("KeyId={0}", keyId);
                Console.WriteLine("PayloadLength={0}", payload.Length);
                return 0;
            }

            if (EqualsIgnoreCase(command, "stfg2-inspect"))
            {
                string file = ReadOption(args, "--file");

                if (string.IsNullOrWhiteSpace(file))
                {
                    Console.Error.WriteLine("Missing required --file option.");
                    return 2;
                }

                byte[] bytes = File.ReadAllBytes(file);

                if (!StateForgeStfg2.IsStfg2(bytes))
                {
                    Console.WriteLine("IsStfg2=False");
                    return 1;
                }

                StateForgeStfg2ReadResult result = StateForgeStfg2.Read(bytes);
                Console.WriteLine("IsStfg2=True");
                Console.WriteLine("Version={0}", result.Version);
                Console.WriteLine("Flags={0}", result.Flags);
                Console.WriteLine("KeyId={0}", result.KeyId);
                Console.WriteLine("ChecksumValid={0}", result.ChecksumValid);
                Console.WriteLine("PayloadLength={0}", result.Payload == null ? 0 : result.Payload.Length);
                Console.WriteLine("Checksum={0}", StateForgeStfg2.ToHex(result.Checksum));
                return result.ChecksumValid ? 0 : 1;
            }

            if (EqualsIgnoreCase(command, "keyring-rotate"))
            {
                string ringFile = ReadOption(args, "--ring");
                string keyId = ReadOption(args, "--new-key-id");
                bool retirePrevious = HasSwitch(args, "--retire-previous");

                if (string.IsNullOrWhiteSpace(ringFile))
                {
                    Console.Error.WriteLine("Missing required --ring option.");
                    return 2;
                }

                StateForgeAesKeyRingRotationResult result = StateForgeAesKeyRingManager.RotateAndSave(ringFile, keyId, retirePrevious);

                Console.WriteLine("Rotated key ring: {0}", ringFile);
                Console.WriteLine("PreviousKeyId={0}", result.PreviousKeyId);
                Console.WriteLine("CurrentKeyId={0}", result.CurrentKeyId);
                Console.WriteLine("KeyCount={0}", result.KeyCount);
                return 0;
            }

            if (EqualsIgnoreCase(command, "keyring-validate"))
            {
                string ringFile = ReadOption(args, "--ring");

                if (string.IsNullOrWhiteSpace(ringFile))
                {
                    Console.Error.WriteLine("Missing required --ring option.");
                    return 2;
                }

                StateForgeAesKeyRing ring = StateForgeAesKeyRingReader.Load(ringFile);
                List<string> errors = StateForgeAesKeyRingManager.Validate(ring);

                if (errors.Count == 0)
                {
                    Console.WriteLine("Success=True");
                    Console.WriteLine("CurrentKeyId={0}", ring.CurrentKeyId);
                    Console.WriteLine("KeyCount={0}", ring.Keys.Count);
                    return 0;
                }

                Console.WriteLine("Success=False");

                foreach (string error in errors)
                {
                    Console.WriteLine("Error={0}", error);
                }

                return 1;
            }

            if (EqualsIgnoreCase(command, "generate-key"))
            {
                int bytes = 32;
                string bytesValue = ReadOption(args, "--bytes");
                int parsedBytes;

                if (int.TryParse(bytesValue, out parsedBytes) && (parsedBytes == 16 || parsedBytes == 24 || parsedBytes == 32))
                {
                    bytes = parsedBytes;
                }

                byte[] key = new byte[bytes];

                using (System.Security.Cryptography.RandomNumberGenerator rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                {
                    rng.GetBytes(key);
                }

                Console.WriteLine(Convert.ToBase64String(key));
                return 0;
            }

            string root = ReadOption(args, "--root");
            string format = ReadOption(args, "--format") ?? "table";

            if (string.IsNullOrWhiteSpace(root))
            {
                Console.Error.WriteLine("Missing required --root option.");
                return 2;
            }

            StateForgeFileStoreOptions options = CreateOptions(root, args);
            StateForgeFileStore store = new StateForgeFileStore(options);

            if (EqualsIgnoreCase(command, "list"))
            {
                List<StateForgeEntryInfo> items = new List<StateForgeEntryInfo>(store.Enumerate());

                if (EqualsIgnoreCase(format, "json"))
                {
                    WriteEntriesJson(items);
                }
                else
                {
                    foreach (StateForgeEntryInfo item in items)
                    {
                        Console.WriteLine("{0}\\tExpires={1:u}\\tLocked={2}\\tBytes={3}\\tCompressed={4}\\tEncrypted={5}\\tAesEncrypted={6}",
                            item.Key,
                            item.ExpiresUtc,
                            item.Locked,
                            item.PayloadLength,
                            item.Compressed,
                            item.Encrypted,
                            item.AesEncrypted);
                    }
                }

                return 0;
            }

            if (EqualsIgnoreCase(command, "validate"))
            {
                StateForgeValidationResult validation = store.ValidateConfiguration();

                if (EqualsIgnoreCase(format, "json"))
                {
                    Console.WriteLine("{\"success\":" + (validation.Success ? "true" : "false") +
                        ",\"errors\":" + StringArrayJson(validation.Errors) +
                        ",\"warnings\":" + StringArrayJson(validation.Warnings) + "}");
                }
                else
                {
                    Console.WriteLine("Success={0}", validation.Success);

                    foreach (string warning in validation.Warnings)
                    {
                        Console.WriteLine("Warning={0}", warning);
                    }

                    foreach (string error in validation.Errors)
                    {
                        Console.WriteLine("Error={0}", error);
                    }
                }

                return validation.Success ? 0 : 1;
            }

            if (EqualsIgnoreCase(command, "health"))
            {
                StateForgeHealthResult health = store.CheckHealth();

                if (EqualsIgnoreCase(format, "json"))
                {
                    Console.WriteLine("{\"healthy\":" + (health.Healthy ? "true" : "false") +
                        ",\"canRead\":" + (health.CanRead ? "true" : "false") +
                        ",\"canWrite\":" + (health.CanWrite ? "true" : "false") +
                        ",\"canLock\":" + (health.CanLock ? "true" : "false") +
                        ",\"canEnumerate\":" + (health.CanEnumerate ? "true" : "false") +
                        ",\"canCleanup\":" + (health.CanCleanup ? "true" : "false") +
                        ",\"errors\":" + StringArrayJson(health.Errors) + "}");
                }
                else
                {
                    Console.WriteLine("Healthy={0}", health.Healthy);
                    Console.WriteLine("CanRead={0}", health.CanRead);
                    Console.WriteLine("CanWrite={0}", health.CanWrite);
                    Console.WriteLine("CanLock={0}", health.CanLock);
                    Console.WriteLine("CanEnumerate={0}", health.CanEnumerate);
                    Console.WriteLine("CanCleanup={0}", health.CanCleanup);

                    foreach (string error in health.Errors)
                    {
                        Console.WriteLine("Error={0}", error);
                    }
                }

                return health.Healthy ? 0 : 1;
            }

            if (EqualsIgnoreCase(command, "stats"))
            {
                StateForgeStoreStats stats = store.GetStats();

                if (EqualsIgnoreCase(format, "json"))
                {
                    Console.WriteLine("{\"totalSessions\":" + stats.TotalSessions +
                        ",\"expiredSessions\":" + stats.ExpiredSessions +
                        ",\"lockedSessions\":" + stats.LockedSessions +
                        ",\"compressedSessions\":" + stats.CompressedSessions +
                        ",\"encryptedSessions\":" + stats.EncryptedSessions +
                        ",\"aesEncryptedSessions\":" + stats.AesEncryptedSessions +
                        ",\"totalPayloadBytes\":" + stats.TotalPayloadBytes +
                        ",\"averagePayloadBytes\":" + stats.AveragePayloadBytes + "}");
                }
                else
                {
                    Console.WriteLine("TotalSessions={0}", stats.TotalSessions);
                    Console.WriteLine("ExpiredSessions={0}", stats.ExpiredSessions);
                    Console.WriteLine("LockedSessions={0}", stats.LockedSessions);
                    Console.WriteLine("CompressedSessions={0}", stats.CompressedSessions);
                    Console.WriteLine("EncryptedSessions={0}", stats.EncryptedSessions);
                    Console.WriteLine("AesEncryptedSessions={0}", stats.AesEncryptedSessions);
                    Console.WriteLine("TotalPayloadBytes={0}", stats.TotalPayloadBytes);
                    Console.WriteLine("AveragePayloadBytes={0}", stats.AveragePayloadBytes);
                }

                return 0;
            }


            if (EqualsIgnoreCase(command, "metrics"))
            {
                StateForgeMetricSnapshot snapshot = StateForgeMetrics.Snapshot();

                if (EqualsIgnoreCase(format, "json"))
                {
                    Console.WriteLine("{\"reads\":" + snapshot.Reads +
                        ",\"writes\":" + snapshot.Writes +
                        ",\"deletes\":" + snapshot.Deletes +
                        ",\"locksAcquired\":" + snapshot.LocksAcquired +
                        ",\"lockContentions\":" + snapshot.LockContentions +
                        ",\"cleanups\":" + snapshot.Cleanups +
                        ",\"quarantines\":" + snapshot.Quarantines +
                        ",\"corruptions\":" + snapshot.Corruptions +
                        ",\"capturedUtc\":\"" + snapshot.CapturedUtc.UtcDateTime.ToString("o") + "\"}");
                }
                else
                {
                    Console.WriteLine("Reads={0}", snapshot.Reads);
                    Console.WriteLine("Writes={0}", snapshot.Writes);
                    Console.WriteLine("Deletes={0}", snapshot.Deletes);
                    Console.WriteLine("LocksAcquired={0}", snapshot.LocksAcquired);
                    Console.WriteLine("LockContentions={0}", snapshot.LockContentions);
                    Console.WriteLine("Cleanups={0}", snapshot.Cleanups);
                    Console.WriteLine("Quarantines={0}", snapshot.Quarantines);
                    Console.WriteLine("Corruptions={0}", snapshot.Corruptions);
                    Console.WriteLine("CapturedUtc={0:o}", snapshot.CapturedUtc);
                }

                return 0;
            }

            if (EqualsIgnoreCase(command, "diag"))
            {
                StateForgeStoreDiagnostics d = store.GetDiagnostics();

                if (EqualsIgnoreCase(format, "json"))
                {
                    Console.WriteLine("{\"rootPath\":\"" + Escape(d.RootPath) + "\",\"sessionFileCount\":" + d.SessionFileCount + ",\"tempFileCount\":" + d.TempFileCount + ",\"backupFileCount\":" + d.BackupFileCount + ",\"quarantineFileCount\":" + d.QuarantineFileCount + "}");
                }
                else
                {
                    Console.WriteLine("RootPath={0}", d.RootPath);
                    Console.WriteLine("Sessions={0}", d.SessionFileCount);
                    Console.WriteLine("Temp={0}", d.TempFileCount);
                    Console.WriteLine("Backups={0}", d.BackupFileCount);
                    Console.WriteLine("Quarantine={0}", d.QuarantineFileCount);
                }

                return 0;
            }

            if (EqualsIgnoreCase(command, "cleanup"))
            {
                StateForgeCleanupResult r = store.CleanupExpired(true);
                Console.WriteLine("ExpiredDeleted={0}; InvalidQuarantined={1}; InvalidDeleted={2}; Failed={3}", r.ExpiredDeleted, r.InvalidQuarantined, r.InvalidDeleted, r.Failed);
                return 0;
            }

            if (EqualsIgnoreCase(command, "remove"))
            {
                string key = ReadOption(args, "--key");

                if (string.IsNullOrWhiteSpace(key))
                {
                    Console.Error.WriteLine("Missing required --key option.");
                    return 2;
                }

                bool removed = store.ForceRemove(key);
                Console.WriteLine("Removed={0}", removed);
                return removed ? 0 : 1;
            }

            Usage();
            return 2;
        }

        private static StateForgeFileStoreOptions CreateOptions(string root, string[] args)
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = root;
            options.EnableEncryption = false;
            options.ProtectionMode = StateForgeProtectionMode.None;
            options.AesKeyBase64 = ReadOption(args, "--aes-key");

            string protection = ReadOption(args, "--protection");

            if (EqualsIgnoreCase(protection, "aes"))
            {
                options.EnableEncryption = true;
                options.ProtectionMode = StateForgeProtectionMode.Aes;
            }
            else if (EqualsIgnoreCase(protection, "dpapi"))
            {
                options.EnableEncryption = true;
                options.ProtectionMode = StateForgeProtectionMode.Dpapi;
            }
            else if (!string.IsNullOrWhiteSpace(options.AesKeyBase64))
            {
                options.EnableEncryption = true;
                options.ProtectionMode = StateForgeProtectionMode.Aes;
            }

            return options;
        }

        private static void WriteEntriesJson(IList<StateForgeEntryInfo> items)
        {
            StringBuilder b = new StringBuilder();
            b.Append("[");

            for (int i = 0; i < items.Count; i++)
            {
                StateForgeEntryInfo item = items[i];

                if (i > 0)
                {
                    b.Append(",");
                }

                b.Append("{");
                b.Append("\"key\":\"").Append(Escape(item.Key)).Append("\",");
                b.Append("\"expiresUtc\":\"").Append(item.ExpiresUtc.UtcDateTime.ToString("o")).Append("\",");
                b.Append("\"locked\":").Append(item.Locked ? "true" : "false").Append(",");
                b.Append("\"payloadLength\":").Append(item.PayloadLength).Append(",");
                b.Append("\"compressed\":").Append(item.Compressed ? "true" : "false").Append(",");
                b.Append("\"encrypted\":").Append(item.Encrypted ? "true" : "false").Append(",");
                b.Append("\"aesEncrypted\":").Append(item.AesEncrypted ? "true" : "false").Append(",");
                b.Append("\"physicalPath\":\"").Append(Escape(item.PhysicalPath)).Append("\"");
                b.Append("}");
            }

            b.Append("]");
            Console.WriteLine(b.ToString());
        }

        private static string StringArrayJson(IEnumerable<string> values)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("[");

            bool first = true;

            foreach (string value in values)
            {
                if (!first)
                {
                    builder.Append(",");
                }

                builder.Append("\"").Append(Escape(value)).Append("\"");
                first = false;
            }

            builder.Append("]");
            return builder.ToString();
        }

        private static bool EqualsIgnoreCase(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadOption(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (EqualsIgnoreCase(args[i], name))
                {
                    return args[i + 1];
                }
            }

            return null;
        }


        private static bool HasSwitch(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (EqualsIgnoreCase(args[i], name))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void Usage()
        {
            Console.WriteLine("StateForge.Tools");
            Console.WriteLine("  list --root D:\\\\StateForge [--format table|json] [--protection none|dpapi|aes] [--aes-key KEY]");
            Console.WriteLine("  diag --root D:\\\\StateForge [--format table|json]");
            Console.WriteLine("  stats --root D:\\\\StateForge [--format table|json] [--protection none|dpapi|aes] [--aes-key KEY]");
            Console.WriteLine("  validate --root D:\\\\StateForge [--format table|json] [--protection none|dpapi|aes] [--aes-key KEY]");
            Console.WriteLine("  health --root D:\\\\StateForge [--format table|json] [--protection none|dpapi|aes] [--aes-key KEY]");
            Console.WriteLine("  cleanup --root D:\\\\StateForge [--protection none|dpapi|aes] [--aes-key KEY]");
            Console.WriteLine("  remove --root D:\\\\StateForge --key SESSIONKEY [--protection none|dpapi|aes] [--aes-key KEY]");
            Console.WriteLine("  metrics [--format table|json]");
            Console.WriteLine("  keyring-create --out FILE [--key-id KEYID]");
            Console.WriteLine("  keyring-generate-key [--key-id KEYID] [--format table|json]");
            Console.WriteLine("  keyring-rotate --ring FILE [--new-key-id KEYID] [--retire-previous]");
            Console.WriteLine("  keyring-validate --ring FILE");
            Console.WriteLine("  stfg2-inspect --file FILE");
            Console.WriteLine("  stfg2-create --out FILE [--key-id KEYID] [--text TEXT]");
            Console.WriteLine("  stfg2-migrate --source FILE --destination FILE [--key-id KEYID] [--overwrite]");
            Console.WriteLine("  stfg2-migrate-store --root PATH [--key-id KEYID] [--pattern PATTERN] (--dry-run|--apply)");
            Console.WriteLine("  generate-key [--bytes 16|24|32]");
        }
    }
}

// Commands: dashboard --root PATH [--replicas "name=PATH;PATH"] [--replica-stale-seconds N]; prometheus [--root PATH]
