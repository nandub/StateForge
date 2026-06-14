using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using StateForge.Core;
using StateForge.FileStore;
using StateForge.Replication;
using StateForge.Snapshots;

namespace StateForge.UpgradeCompatibilityTests
{
    internal static class Program
    {
        private const string AesKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

        private static int Main()
        {
            string root = Path.Combine(Path.GetTempPath(), "StateForgeUpgradeCompatibilityTests");

            try
            {
                ResetDirectory(root);

                TestSameLayoutMixedVersion(Path.Combine(root, "same-layout"));
                TestShardTransition(Path.Combine(root, "shard-transition"));
                TestLegacyReplication(Path.Combine(root, "replication"));
                TestLegacySnapshotRestore(Path.Combine(root, "snapshot"));
                TestUnsupportedDowngradeBoundaries(Path.Combine(root, "boundaries"));

                Console.WriteLine("PASS: legacy writer to current reader");
                Console.WriteLine("PASS: current refresh visible to legacy reader");
                Console.WriteLine("PASS: current writer to legacy reader");
                Console.WriteLine("PASS: mixed-version remove");
                Console.WriteLine("PASS: shard fallback read and remove");
                Console.WriteLine("PASS: post-drain shard migration");
                Console.WriteLine("PASS: legacy replication to current reader");
                Console.WriteLine("PASS: legacy snapshot restore to current reader");
                Console.WriteLine("PASS: AES downgrade boundary");
                Console.WriteLine("PASS: STFG2 offline migration boundary");

                Directory.Delete(root, true);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static void TestSameLayoutMixedVersion(string root)
        {
            ResetDirectory(root);
            LegacyStoreV1 legacy = new LegacyStoreV1(root);
            byte[] oldPayload = Encoding.UTF8.GetBytes("legacy-session");
            DateTimeOffset oldExpiry = DateTimeOffset.UtcNow.AddMinutes(10);
            legacy.Write("mixed-key", oldPayload, oldExpiry, false);

            StateForgeFileStore current = CreateStore(root, 0, false);
            StateForgeEntry oldEntry = current.Get("mixed-key");
            Require(oldEntry != null, "Current reader could not read a legacy STFG1 record.");
            Require(Equal(oldPayload, oldEntry.Value), "Legacy payload changed during current read.");

            Require(current.Refresh("mixed-key", TimeSpan.FromMinutes(30)), "Current refresh failed.");
            LegacyEntry refreshed = legacy.Read("mixed-key");
            Require(refreshed != null, "Legacy reader could not read the refreshed record.");
            Require(refreshed.ExpiresUtc > oldExpiry, "Current refresh was not visible to the legacy reader.");

            byte[] currentPayload = Encoding.UTF8.GetBytes("current-session");
            current.Set("mixed-key", currentPayload, TimeSpan.FromMinutes(20));
            LegacyEntry currentEntry = legacy.Read("mixed-key");
            Require(currentEntry != null, "Legacy reader could not read a current STFG1 record.");
            Require(Equal(currentPayload, currentEntry.Value), "Current payload changed during legacy read.");

            Require(current.Remove("mixed-key"), "Current remove failed.");
            Require(legacy.Read("mixed-key") == null, "Legacy reader still found a removed record.");
        }

        private static void TestShardTransition(string root)
        {
            ResetDirectory(root);
            LegacyStoreV1 legacy = new LegacyStoreV1(root);
            byte[] payload = Encoding.UTF8.GetBytes("shard-transition");
            legacy.Write("shard-key", payload, DateTimeOffset.UtcNow.AddMinutes(10), false);

            StateForgeFileStore sharded = CreateStore(root, 2, false);
            Require(sharded.Get("shard-key") != null, "Sharded current reader could not use legacy fallback.");
            Require(sharded.Remove("shard-key"), "Sharded current remove could not delete a legacy record.");
            Require(legacy.Read("shard-key") == null, "Legacy record remained after fallback remove.");

            legacy.Write("migrate-key", payload, DateTimeOffset.UtcNow.AddMinutes(10), false);
            StateForgeEntry migrationEntry = sharded.Get("migrate-key");
            Require(migrationEntry != null, "Migration fallback read failed.");
            Require(sharded.Remove("migrate-key"), "Migration cleanup failed.");
            sharded.Set("migrate-key", migrationEntry.Value, TimeSpan.FromMinutes(10));

            Require(sharded.Get("migrate-key") != null, "Migrated sharded record is unreadable.");
            Require(legacy.Read("migrate-key") == null, "Legacy reader unexpectedly found a depth-two record.");
            Require(Directory.GetFiles(Path.Combine(root, "sessions"), "*.stfg", SearchOption.TopDirectoryOnly).Length == 0,
                "Legacy root records remain after shard migration.");
        }

        private static void TestLegacyReplication(string root)
        {
            string primary = Path.Combine(root, "primary");
            string replica = Path.Combine(root, "replica");
            ResetDirectory(primary);
            Directory.CreateDirectory(replica);

            LegacyStoreV1 legacy = new LegacyStoreV1(primary);
            byte[] payload = Encoding.UTF8.GetBytes("legacy-replication");
            legacy.Write("replication-key", payload, DateTimeOffset.UtcNow.AddMinutes(10), false);

            StateForgeReplicationOptions options = new StateForgeReplicationOptions();
            options.PrimaryRootPath = primary;
            options.Replicas.Add(new StateForgeReplicaNode { Name = "upgrade-replica", RootPath = replica });

            StateForgeReplicationResult result = new StateForgeFileReplicator().Replicate(options);
            Require(result.Success && result.FilesCopied == 1, "Legacy record replication failed.");

            StateForgeEntry replicated = CreateStore(replica, 0, false).Get("replication-key");
            Require(replicated != null && Equal(payload, replicated.Value),
                "Current reader could not read the replicated legacy record.");
        }

        private static void TestLegacySnapshotRestore(string root)
        {
            string primary = Path.Combine(root, "primary");
            string repository = Path.Combine(root, "repository");
            string restore = Path.Combine(root, "restore");
            ResetDirectory(primary);

            LegacyStoreV1 legacy = new LegacyStoreV1(primary);
            byte[] payload = Encoding.UTF8.GetBytes("legacy-snapshot");
            legacy.Write("snapshot-key", payload, DateTimeOffset.UtcNow.AddMinutes(10), true);

            StateForgeSnapshotService service = new StateForgeSnapshotService();
            StateForgeSnapshotOptions options = new StateForgeSnapshotOptions();
            options.SourceRootPath = primary;
            options.SnapshotRepositoryPath = repository;
            options.SnapshotName = "legacy-snapshot";

            StateForgeSnapshotResult snapshot = service.Create(options);
            Require(snapshot.Success && snapshot.FilesCopied == 1, "Legacy snapshot creation failed.");

            StateForgeSnapshotResult restored = service.Restore(snapshot.SnapshotPath, restore, true);
            Require(restored.Success && restored.FilesCopied == 1, "Legacy snapshot restore failed.");

            StateForgeEntry restoredEntry = CreateStore(restore, 0, false).Get("snapshot-key");
            Require(restoredEntry != null && Equal(payload, restoredEntry.Value),
                "Current reader could not read the restored compressed legacy record.");
        }

        private static void TestUnsupportedDowngradeBoundaries(string root)
        {
            string aesRoot = Path.Combine(root, "aes");
            ResetDirectory(aesRoot);

            StateForgeFileStore aesStore = CreateStore(aesRoot, 0, true);
            aesStore.Set("aes-key", Encoding.UTF8.GetBytes("aes-current"), TimeSpan.FromMinutes(10));
            Require(aesStore.Get("aes-key") != null, "Current AES record is unreadable by the current store.");

            bool aesRejected = false;
            try
            {
                new LegacyStoreV1(aesRoot).Read("aes-key");
            }
            catch (NotSupportedException)
            {
                aesRejected = true;
            }

            Require(aesRejected, "Legacy reader did not reject the AES downgrade boundary.");

            string stfg2Root = Path.Combine(root, "stfg2");
            ResetDirectory(stfg2Root);
            LegacyStoreV1 legacy = new LegacyStoreV1(stfg2Root);
            legacy.Write("stfg2-key", Encoding.UTF8.GetBytes("offline-envelope"), DateTimeOffset.UtcNow.AddMinutes(10), false);
            string livePath = legacy.GetPath("stfg2-key");
            byte[] original = File.ReadAllBytes(livePath);
            byte[] wrapped = StateForgeStfg2Envelope.Wrap(original, false, false, false, false, "upgrade-key");
            File.WriteAllBytes(livePath, wrapped);

            StateForgeStfg2EnvelopeResult envelope = StateForgeStfg2Envelope.Unwrap(wrapped);
            Require(envelope.IsStfg2 && envelope.ChecksumValid && Equal(original, envelope.Payload),
                "STFG2 offline envelope did not preserve the STFG1 record.");
            Require(CreateStore(stfg2Root, 0, false).Get("stfg2-key") == null,
                "Live FileStore unexpectedly accepted an offline STFG2 envelope.");
        }

        private static StateForgeFileStore CreateStore(string root, int shardDepth, bool aes)
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = root;
            options.ShardDepth = shardDepth;
            options.EnableEncryption = aes;
            options.ProtectionMode = aes ? StateForgeProtectionMode.Aes : StateForgeProtectionMode.None;
            options.AesKeyBase64 = aes ? AesKey : string.Empty;
            return new StateForgeFileStore(options);
        }

        private static void ResetDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }

            Directory.CreateDirectory(path);
        }

        private static bool Equal(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private sealed class LegacyEntry
        {
            public byte[] Value { get; set; }

            public DateTimeOffset ExpiresUtc { get; set; }
        }

        private sealed class LegacyStoreV1
        {
            private readonly string _sessionsPath;

            public LegacyStoreV1(string rootPath)
            {
                _sessionsPath = Path.Combine(rootPath, "sessions");
                Directory.CreateDirectory(_sessionsPath);
            }

            public string GetPath(string key)
            {
                return Path.Combine(_sessionsPath, Hash(key) + ".stfg");
            }

            public void Write(string key, byte[] value, DateTimeOffset expiresUtc, bool compressed)
            {
                string path = GetPath(key);
                byte[] stored = compressed ? Compress(value) : value;
                int flags = compressed ? StateForgeConstants.FlagCompressed : 0;
                DateTimeOffset now = DateTimeOffset.UtcNow;

                using (FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(StateForgeConstants.FileMagic);
                    writer.Write(StateForgeConstants.FileVersion);
                    writer.Write(flags);
                    writer.Write(key);
                    writer.Write(now.ToUnixTimeMilliseconds());
                    writer.Write(now.ToUnixTimeMilliseconds());
                    writer.Write(expiresUtc.ToUnixTimeMilliseconds());
                    writer.Write(false);
                    writer.Write((long)0);
                    writer.Write(false);
                    writer.Write(stored.Length);
                    writer.Write(stored);
                }
            }

            public LegacyEntry Read(string key)
            {
                string path = GetPath(key);
                if (!File.Exists(path))
                {
                    return null;
                }

                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int magic = reader.ReadInt32();
                    int version = reader.ReadInt32();
                    int flags = reader.ReadInt32();

                    if (magic != StateForgeConstants.FileMagic || version != StateForgeConstants.FileVersion)
                    {
                        throw new NotSupportedException("Legacy reader supports only STFG1 store records.");
                    }

                    if ((flags & (StateForgeConstants.FlagEncrypted | StateForgeConstants.FlagAesEncrypted)) != 0)
                    {
                        throw new NotSupportedException("Legacy reader does not support encrypted records.");
                    }

                    reader.ReadString();
                    reader.ReadInt64();
                    reader.ReadInt64();
                    DateTimeOffset expiresUtc = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
                    reader.ReadBoolean();
                    reader.ReadInt64();
                    bool hasLockDate = reader.ReadBoolean();
                    if (hasLockDate)
                    {
                        reader.ReadInt64();
                    }

                    int length = reader.ReadInt32();
                    byte[] value = reader.ReadBytes(length);
                    if ((flags & StateForgeConstants.FlagCompressed) != 0)
                    {
                        value = Decompress(value);
                    }

                    return new LegacyEntry { Value = value, ExpiresUtc = expiresUtc };
                }
            }

            private static string Hash(string key)
            {
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key ?? string.Empty));
                    StringBuilder builder = new StringBuilder(bytes.Length * 2);
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        builder.Append(bytes[i].ToString("X2"));
                    }

                    return builder.ToString();
                }
            }

            private static byte[] Compress(byte[] value)
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress))
                    {
                        gzip.Write(value, 0, value.Length);
                    }

                    return output.ToArray();
                }
            }

            private static byte[] Decompress(byte[] value)
            {
                using (MemoryStream input = new MemoryStream(value))
                using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
                using (MemoryStream output = new MemoryStream())
                {
                    gzip.CopyTo(output);
                    return output.ToArray();
                }
            }
        }
    }
}
