using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using StateForge.Core;
using StateForge.FileStore;
using StateForge.Security;

namespace StateForge.SecurityTests
{
    internal static class Program
    {
        private const string PrimaryKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
        private const string WrongKey = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBA=";

        private static int Main()
        {
            string root = Path.Combine(Path.GetTempPath(), "StateForgeSecurityTests");

            try
            {
                ResetDirectory(root);

                TestAuthenticatedRoundTrip(Path.Combine(root, "round-trip"));
                TestTamperRejection(Path.Combine(root, "tamper"));
                TestAuthenticationFlagStripping(Path.Combine(root, "flag-stripping"));
                TestWrongKeyRejection(Path.Combine(root, "wrong-key"));
                TestLegacyAesRead(Path.Combine(root, "legacy"));
                TestCompressedExpansionLimit(Path.Combine(root, "compression-limit"));
                TestValidatedAtomicKeyRingSave(Path.Combine(root, "key-ring"));

                Console.WriteLine("PASS: authenticated AES round-trip");
                Console.WriteLine("PASS: AES metadata tamper rejection");
                Console.WriteLine("PASS: AES ciphertext tamper rejection");
                Console.WriteLine("PASS: AES authentication tag tamper rejection");
                Console.WriteLine("PASS: authentication flag stripping rejection");
                Console.WriteLine("PASS: wrong AES key rejection");
                Console.WriteLine("PASS: legacy AES record compatibility");
                Console.WriteLine("PASS: compressed payload expansion limit");
                Console.WriteLine("PASS: validated atomic key-ring save");

                Directory.Delete(root, true);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static void TestAuthenticatedRoundTrip(string root)
        {
            StateForgeFileStore store = CreateAesStore(root, PrimaryKey);
            byte[] payload = Encoding.UTF8.GetBytes("authenticated-session");
            store.Set("round-trip", payload, TimeSpan.FromMinutes(10));

            StateForgeEntry entry = store.Get("round-trip");
            Require(entry != null && Equal(payload, entry.Value), "Authenticated AES round-trip failed.");

            byte[] record = File.ReadAllBytes(store.Enumerate().Single().PhysicalPath);
            int flags = BitConverter.ToInt32(record, 8);
            Require((flags & StateForgeConstants.FlagAesEncrypted) != 0, "AES flag is missing.");
            Require((flags & StateForgeConstants.FlagAuthenticated) != 0, "Authentication flag is missing.");

            store.Set("empty", new byte[0], TimeSpan.FromMinutes(10));
            StateForgeEntry empty = store.Get("empty");
            Require(empty != null && empty.Value.Length == 0, "Authenticated empty AES record failed.");
            byte[] emptyRecord = File.ReadAllBytes(store.Enumerate().Single(item => item.Key == "empty").PhysicalPath);
            Require((BitConverter.ToInt32(emptyRecord, 8) & StateForgeConstants.FlagAuthenticated) != 0,
                "Empty AES record is not authenticated.");
        }

        private static void TestTamperRejection(string root)
        {
            AssertTamperRejected(Path.Combine(root, "metadata"), delegate(byte[] record)
            {
                int expiresOffset = GetRecordOffsets(record).ExpiresOffset;
                record[expiresOffset] ^= 0x01;
            });

            AssertTamperRejected(Path.Combine(root, "ciphertext"), delegate(byte[] record)
            {
                int payloadOffset = GetRecordOffsets(record).PayloadOffset;
                record[payloadOffset + 16] ^= 0x01;
            });

            AssertTamperRejected(Path.Combine(root, "tag"), delegate(byte[] record)
            {
                record[record.Length - 2] ^= 0x01;
            });
        }

        private static void TestAuthenticationFlagStripping(string root)
        {
            AssertTamperRejected(root, delegate(byte[] record)
            {
                int flags = BitConverter.ToInt32(record, 8);
                byte[] stripped = BitConverter.GetBytes(flags & ~StateForgeConstants.FlagAuthenticated);
                Buffer.BlockCopy(stripped, 0, record, 8, stripped.Length);
            });
        }

        private static void TestWrongKeyRejection(string root)
        {
            StateForgeFileStore writer = CreateAesStore(root, PrimaryKey);
            writer.Set("wrong-key", Encoding.UTF8.GetBytes("secret"), TimeSpan.FromMinutes(10));

            StateForgeFileStore reader = CreateAesStore(root, WrongKey);
            Require(reader.Get("wrong-key") == null, "A record was accepted with the wrong AES key.");
        }

        private static void TestLegacyAesRead(string root)
        {
            ResetDirectory(root);
            string sessions = Path.Combine(root, "sessions");
            Directory.CreateDirectory(sessions);

            string key = "legacy-aes";
            byte[] payload = Encoding.UTF8.GetBytes("legacy-cbc-record");
            byte[] encrypted = LegacyProtect(payload, PrimaryKey);
            string path = Path.Combine(sessions, Hash(key) + ".stfg");
            DateTimeOffset now = DateTimeOffset.UtcNow;

            using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(StateForgeConstants.FileMagic);
                writer.Write(StateForgeConstants.FileVersion);
                writer.Write(StateForgeConstants.FlagAesEncrypted);
                writer.Write(key);
                writer.Write(now.ToUnixTimeMilliseconds());
                writer.Write(now.ToUnixTimeMilliseconds());
                writer.Write(now.AddMinutes(10).ToUnixTimeMilliseconds());
                writer.Write(false);
                writer.Write((long)0);
                writer.Write(false);
                writer.Write(encrypted.Length);
                writer.Write(encrypted);
            }

            StateForgeEntry entry = CreateAesStore(root, PrimaryKey).Get(key);
            Require(entry != null && Equal(payload, entry.Value), "Current reader rejected a legacy AES record.");
        }

        private static void TestValidatedAtomicKeyRingSave(string root)
        {
            ResetDirectory(root);
            string path = Path.Combine(root, "stateforge-keyring.json");
            StateForgeAesKeyRing ring = StateForgeAesKeyRingManager.CreateNew("key-001");
            StateForgeAesKeyRingManager.Save(path, ring);
            string original = File.ReadAllText(path);

            StateForgeAesKeyInfo duplicate = StateForgeAesKeyRingManager.CreateKey("key-001");
            ring.Keys.Add(duplicate);

            bool rejected = false;
            try
            {
                StateForgeAesKeyRingManager.Save(path, ring);
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            Require(rejected, "Invalid key ring was not rejected.");
            Require(string.Equals(original, File.ReadAllText(path), StringComparison.Ordinal),
                "Invalid key-ring save changed the existing file.");
            Require(Directory.GetFiles(root, "*.tmp", SearchOption.AllDirectories).Length == 0,
                "Key-ring save left a temporary file.");
        }

        private static void TestCompressedExpansionLimit(string root)
        {
            ResetDirectory(root);
            string sessions = Path.Combine(root, "sessions");
            Directory.CreateDirectory(sessions);
            string key = "compression-limit";
            byte[] compressed = Compress(new byte[4096]);
            string path = Path.Combine(sessions, Hash(key) + ".stfg");
            DateTimeOffset now = DateTimeOffset.UtcNow;

            using (FileStream stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(StateForgeConstants.FileMagic);
                writer.Write(StateForgeConstants.FileVersion);
                writer.Write(StateForgeConstants.FlagCompressed);
                writer.Write(key);
                writer.Write(now.ToUnixTimeMilliseconds());
                writer.Write(now.ToUnixTimeMilliseconds());
                writer.Write(now.AddMinutes(10).ToUnixTimeMilliseconds());
                writer.Write(false);
                writer.Write((long)0);
                writer.Write(false);
                writer.Write(compressed.Length);
                writer.Write(compressed);
            }

            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = root;
            options.MaxPayloadBytes = 64;
            Require(new StateForgeFileStore(options).Get(key) == null,
                "Compressed payload expansion bypassed MaxPayloadBytes.");
        }

        private static void AssertTamperRejected(string root, Action<byte[]> tamper)
        {
            StateForgeFileStore store = CreateAesStore(root, PrimaryKey);
            store.Set("tamper-key", Encoding.UTF8.GetBytes("tamper-payload"), TimeSpan.FromMinutes(10));
            string path = store.Enumerate().Single().PhysicalPath;
            byte[] record = File.ReadAllBytes(path);
            tamper(record);
            File.WriteAllBytes(path, record);

            Require(store.Get("tamper-key") == null, "Tampered AES record was accepted: " + root);
        }

        private static RecordOffsets GetRecordOffsets(byte[] record)
        {
            using (MemoryStream stream = new MemoryStream(record, false))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadInt32();
                reader.ReadString();
                reader.ReadInt64();
                reader.ReadInt64();
                int expiresOffset = checked((int)stream.Position);
                reader.ReadInt64();
                reader.ReadBoolean();
                reader.ReadInt64();
                bool hasLockDate = reader.ReadBoolean();
                if (hasLockDate)
                {
                    reader.ReadInt64();
                }

                reader.ReadInt32();
                return new RecordOffsets(expiresOffset, checked((int)stream.Position));
            }
        }

        private static StateForgeFileStore CreateAesStore(string root, string key)
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = root;
            options.EnableEncryption = true;
            options.ProtectionMode = StateForgeProtectionMode.Aes;
            options.AesKeyBase64 = key;
            return new StateForgeFileStore(options);
        }

        private static byte[] LegacyProtect(byte[] value, string keyBase64)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Convert.FromBase64String(keyBase64);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (MemoryStream output = new MemoryStream())
                {
                    output.Write(aes.IV, 0, aes.IV.Length);
                    using (CryptoStream crypto = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
                    {
                        crypto.Write(value, 0, value.Length);
                        crypto.FlushFinalBlock();
                    }

                    return output.ToArray();
                }
            }
        }

        private static byte[] Compress(byte[] value)
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (System.IO.Compression.GZipStream gzip =
                    new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress))
                {
                    gzip.Write(value, 0, value.Length);
                }

                return output.ToArray();
            }
        }

        private static string Hash(string key)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                StringBuilder builder = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("X2"));
                }

                return builder.ToString();
            }
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

        private sealed class RecordOffsets
        {
            public RecordOffsets(int expiresOffset, int payloadOffset)
            {
                ExpiresOffset = expiresOffset;
                PayloadOffset = payloadOffset;
            }

            public int ExpiresOffset { get; private set; }

            public int PayloadOffset { get; private set; }
        }
    }
}
