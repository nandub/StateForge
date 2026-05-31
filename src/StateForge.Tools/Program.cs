using System;
using System.Collections.Generic;
using System.Text;
using StateForge.Core;
using StateForge.FileStore;
using StateForge.Telemetry;
using StateForge.Security;

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
            Console.WriteLine("  generate-key [--bytes 16|24|32]");
        }
    }
}
