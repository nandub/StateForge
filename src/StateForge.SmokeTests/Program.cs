using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Caching.Distributed;
using StateForge.AspNetCore;
using StateForge.Core;
using StateForge.FileStore;

namespace StateForge.SmokeTests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string root = ReadOption(args, "--root");
            bool keep = HasSwitch(args, "--keep");
            bool skipDemo = HasSwitch(args, "--skip-demo");

            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.Combine(Path.GetTempPath(), "StateForgeSmokeTests", Guid.NewGuid().ToString("N"));
            }

            List<SmokeResult> results = new List<SmokeResult>();

            results.Add(Run("FileStore round-trip", delegate { TestFileStoreRoundTrip(root); }));
            results.Add(Run("Persistence across store recreation", delegate { TestPersistenceAcrossStoreRecreation(root); }));
            results.Add(Run("Compression round-trip", delegate { TestCompression(root); }));
            results.Add(Run("DPAPI encryption round-trip", delegate { TestEncryption(root); }));
            results.Add(Run("AES encryption round-trip", delegate { TestAesEncryption(root); }));
            results.Add(Run("Compression plus encryption round-trip", delegate { TestCompressionAndEncryption(root); }));
            results.Add(Run("Lock contention", delegate { TestLockContention(root); }));
            results.Add(Run("Stale lock recovery", delegate { TestStaleLockRecovery(root); }));
            results.Add(Run("Expiration cleanup", delegate { TestExpirationCleanup(root); }));
            results.Add(Run("Corruption quarantine", delegate { TestCorruptionQuarantine(root); }));
            results.Add(Run("ASP.NET Core IDistributedCache adapter", delegate { TestAspNetCoreDistributedCache(root); }));

            if (!skipDemo)
            {
                results.Add(Run("Consolidated demo store", delegate { CreateDemoStore(root); }));
            }

            Console.WriteLine();
            Console.WriteLine("StateForge Smoke Test Summary");
            Console.WriteLine("-----------------------------");

            foreach (SmokeResult result in results)
            {
                Console.WriteLine("{0}: {1}", result.Passed ? "PASS" : "FAIL", result.Name);

                if (!result.Passed)
                {
                    Console.WriteLine("      {0}", result.Error);
                }
            }

            Console.WriteLine();
            Console.WriteLine("RootPath: {0}", root);
            Console.WriteLine();
            Console.WriteLine("Store inspection paths");
            Console.WriteLine("----------------------");
            Console.WriteLine("roundtrip        : {0}", MakeRoot(root, "roundtrip"));
            Console.WriteLine("recreate         : {0}", MakeRoot(root, "recreate"));
            Console.WriteLine("compression      : {0}", MakeRoot(root, "compression"));
            Console.WriteLine("encryption       : {0}", MakeRoot(root, "encryption"));
            Console.WriteLine("both             : {0}", MakeRoot(root, "both"));
            Console.WriteLine("quarantine       : {0}", MakeRoot(root, "quarantine"));
            Console.WriteLine("aspnetcore-cache : {0}", MakeRoot(root, "aspnetcore-cache"));
            Console.WriteLine("demo             : {0}", MakeRoot(root, "demo"));

            if (keep)
            {
                Console.WriteLine();
                Console.WriteLine("Smoke test AES key");
                Console.WriteLine("------------------");
                Console.WriteLine("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
                Console.WriteLine();
                Console.WriteLine("Example inspection commands");
                Console.WriteLine("---------------------------");
                Console.WriteLine("dotnet run --project .\\src\\StateForge.Tools\\StateForge.Tools.csproj -- diag --root \"{0}\"", MakeRoot(root, "demo"));
                Console.WriteLine("dotnet run --project .\\src\\StateForge.Tools\\StateForge.Tools.csproj -- list --root \"{0}\" --format json", MakeRoot(root, "demo"));
                Console.WriteLine("dotnet run --project .\\src\\StateForge.Tools\\StateForge.Tools.csproj -- list --root \"{0}\" --format json --protection aes --aes-key \"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=\"", MakeRoot(root, "demo"));
                Console.WriteLine("dotnet run --project .\\src\\StateForge.Tools\\StateForge.Tools.csproj -- stats --root \"{0}\" --format json --protection aes --aes-key \"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=\"", MakeRoot(root, "demo"));
            }
            else
            {
                TryDeleteDirectory(root);
                Console.WriteLine("Smoke-test files removed. Use --keep to retain files.");
            }

            int failures = results.Count(r => !r.Passed);
            return failures == 0 ? 0 : 1;
        }

        private static void TestFileStoreRoundTrip(string root)
        {
            string testRoot = MakeRoot(root, "roundtrip");
            StateForgeFileStore store = CreateStore(testRoot, false, false);

            byte[] payload = new byte[] { 1, 2, 3, 4 };
            store.Set("session-1", payload, TimeSpan.FromMinutes(20));

            StateForgeEntry entry = store.Get("session-1");

            Require(entry != null, "Entry was null.");
            Require(BytesEqual(payload, entry.Value), "Payload mismatch.");
        }

        private static void TestPersistenceAcrossStoreRecreation(string root)
        {
            string testRoot = MakeRoot(root, "recreate");

            byte[] payload = new byte[] { 9, 8, 7 };

            StateForgeFileStore first = CreateStore(testRoot, false, false);
            first.Set("session-recreate", payload, TimeSpan.FromMinutes(20));

            StateForgeFileStore second = CreateStore(testRoot, false, false);
            StateForgeEntry entry = second.Get("session-recreate");

            Require(entry != null, "Entry was null after recreating store.");
            Require(BytesEqual(payload, entry.Value), "Payload mismatch after recreating store.");
        }

        private static void TestCompression(string root)
        {
            string testRoot = MakeRoot(root, "compression");
            StateForgeFileStore store = CreateStore(testRoot, true, false);

            byte[] payload = RepeatedByte(4096, 65);
            store.Set("compressed", payload, TimeSpan.FromMinutes(20));

            StateForgeEntry entry = store.Get("compressed");
            Require(entry != null, "Compressed entry was null.");
            Require(BytesEqual(payload, entry.Value), "Compressed payload mismatch.");
            Require(store.Enumerate().First().Compressed, "Metadata did not mark entry as compressed.");
        }

        private static void TestEncryption(string root)
        {
            string testRoot = MakeRoot(root, "encryption");
            StateForgeFileStore store = CreateStore(testRoot, false, true);

            byte[] payload = new byte[] { 10, 20, 30, 40 };
            store.Set("encrypted", payload, TimeSpan.FromMinutes(20));

            StateForgeEntry entry = store.Get("encrypted");
            Require(entry != null, "Encrypted entry was null.");
            Require(BytesEqual(payload, entry.Value), "Encrypted payload mismatch.");
            Require(store.Enumerate().First().Encrypted, "Metadata did not mark entry as encrypted.");
        }


        private static void TestAesEncryption(string root)
        {
            string testRoot = MakeRoot(root, "aes-encryption");
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = testRoot;
            options.EnableEncryption = true;
            options.ProtectionMode = StateForgeProtectionMode.Aes;
            options.AesKeyBase64 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

            StateForgeFileStore store = new StateForgeFileStore(options);
            byte[] payload = new byte[] { 11, 22, 33, 44 };

            store.Set("aes-encrypted", payload, TimeSpan.FromMinutes(20));

            StateForgeEntry entry = store.Get("aes-encrypted");
            Require(entry != null, "AES encrypted entry was null.");
            Require(BytesEqual(payload, entry.Value), "AES encrypted payload mismatch.");
            Require(store.Enumerate().First().AesEncrypted, "Metadata did not mark entry as AES encrypted.");
        }

        private static void TestCompressionAndEncryption(string root)
        {
            string testRoot = MakeRoot(root, "both");
            StateForgeFileStore store = CreateStore(testRoot, true, true);

            byte[] payload = RepeatedByte(8192, 90);
            store.Set("both", payload, TimeSpan.FromMinutes(20));

            StateForgeEntry entry = store.Get("both");
            Require(entry != null, "Compressed/encrypted entry was null.");
            Require(BytesEqual(payload, entry.Value), "Compressed/encrypted payload mismatch.");

            StateForgeEntryInfo info = store.Enumerate().First();
            Require(info.Compressed, "Metadata did not mark entry as compressed.");
            Require(info.Encrypted, "Metadata did not mark entry as encrypted.");
        }

        private static void TestLockContention(string root)
        {
            string testRoot = MakeRoot(root, "lock-contention");
            StateForgeFileStore store = CreateStore(testRoot, false, false);

            store.Set("locked", new byte[] { 1 }, TimeSpan.FromMinutes(20));

            StateForgeLockResult first = store.GetAndLock("locked", TimeSpan.FromMinutes(5));
            Require(first.Found, "First lock did not find entry.");
            Require(!first.LockedByOtherRequest, "First lock was unexpectedly blocked.");

            StateForgeLockResult second = store.GetAndLock("locked", TimeSpan.FromMinutes(5));
            Require(second.Found, "Second lock did not find entry.");
            Require(second.LockedByOtherRequest, "Second lock should have been blocked.");
        }

        private static void TestStaleLockRecovery(string root)
        {
            string testRoot = MakeRoot(root, "stale-lock");
            StateForgeFileStore store = CreateStore(testRoot, false, false);

            store.Set("stale", new byte[] { 1 }, TimeSpan.FromMinutes(20));

            StateForgeLockResult first = store.GetAndLock("stale", TimeSpan.FromMinutes(5));
            Require(first.Found, "First lock did not find entry.");

            StateForgeLockResult second = store.GetAndLock("stale", TimeSpan.Zero);
            Require(second.Found, "Stale-lock recovery did not find entry.");
            Require(!second.LockedByOtherRequest, "Stale lock was not recovered.");
            Require(second.LockId > first.LockId, "LockId was not advanced after stale-lock recovery.");
        }

        private static void TestExpirationCleanup(string root)
        {
            string testRoot = MakeRoot(root, "expiration");
            StateForgeFileStore store = CreateStore(testRoot, false, false);

            store.Set("expired", new byte[] { 1 }, TimeSpan.FromMilliseconds(1));
            System.Threading.Thread.Sleep(50);

            StateForgeCleanupResult result = store.CleanupExpired(true);
            Require(result.ExpiredDeleted >= 1, "Expired entry was not deleted.");
        }

        private static void TestCorruptionQuarantine(string root)
        {
            string testRoot = MakeRoot(root, "quarantine");
            StateForgeFileStore store = CreateStore(testRoot, false, false);

            string badDir = Path.Combine(testRoot, "sessions", "AA");
            Directory.CreateDirectory(badDir);
            File.WriteAllText(Path.Combine(badDir, "bad.stfg"), "garbage");

            StateForgeCleanupResult result = store.CleanupExpired(true);
            Require(result.InvalidQuarantined >= 1, "Invalid file was not quarantined.");
        }

        private static void TestAspNetCoreDistributedCache(string root)
        {
            string testRoot = MakeRoot(root, "aspnetcore-cache");

            StateForgeDistributedCacheOptions options = new StateForgeDistributedCacheOptions();
            options.RootPath = testRoot;
            options.EnableCompression = true;
            options.EnableEncryption = false;

            StateForgeFileStore store = new StateForgeFileStore(options);
            StateForgeDistributedCache cache = new StateForgeDistributedCache(store, options);

            byte[] payload = new byte[] { 100, 101, 102 };
            DistributedCacheEntryOptions cacheOptions = new DistributedCacheEntryOptions();
            cacheOptions.SlidingExpiration = TimeSpan.FromMinutes(20);

            cache.Set("cache-key", payload, cacheOptions);

            byte[] read = cache.Get("cache-key");
            Require(BytesEqual(payload, read), "IDistributedCache payload mismatch.");

            DistributedCacheEntryOptions cappedOptions = new DistributedCacheEntryOptions();
            cappedOptions.SlidingExpiration = TimeSpan.FromMinutes(20);
            cappedOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
            cache.Set("cache-capped", payload, cappedOptions);

            StateForgeEntry capped = store.Get("cache-capped");
            Require(capped != null, "Capped cache entry missing.");
            Require(capped.ExpiresUtc <= DateTimeOffset.UtcNow.AddMinutes(2).AddSeconds(1),
                "Absolute cache expiration did not cap sliding expiration.");

            DateTimeOffset originalCap = capped.ExpiresUtc;
            cache.Refresh("cache-capped");
            StateForgeEntry refreshed = store.Get("cache-capped");
            Require(refreshed != null && refreshed.ExpiresUtc <= originalCap.AddSeconds(1),
                "Cache refresh extended the absolute expiration cap.");

            DistributedCacheEntryOptions absoluteOnlyOptions = new DistributedCacheEntryOptions();
            absoluteOnlyOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            cache.Set("cache-absolute-only", payload, absoluteOnlyOptions);
            StateForgeEntry absoluteOnly = store.Get("cache-absolute-only");
            Require(absoluteOnly != null && absoluteOnly.ExpiresUtc >= DateTimeOffset.UtcNow.AddMinutes(59),
                "Absolute-only cache expiration was capped by the default duration.");

            DistributedCacheEntryOptions expiredOptions = new DistributedCacheEntryOptions();
            expiredOptions.AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(-1);
            cache.Set("cache-expired", payload, expiredOptions);
            Require(cache.Get("cache-expired") == null, "Past absolute expiration created a cache entry.");
        }


        private static void CreateDemoStore(string root)
        {
            string demoRoot = MakeRoot(root, "demo");

            StateForgeFileStore plain = CreateStore(demoRoot, false, false);
            plain.Set("demo-plain", new byte[] { 1, 2, 3 }, TimeSpan.FromMinutes(60));

            StateForgeFileStore compressed = CreateStore(demoRoot, true, false);
            compressed.Set("demo-compressed", RepeatedByte(4096, 65), TimeSpan.FromMinutes(60));

            StateForgeFileStore encrypted = CreateStore(demoRoot, false, true);
            encrypted.Set("demo-encrypted", new byte[] { 9, 8, 7, 6 }, TimeSpan.FromMinutes(60));

            StateForgeFileStore both = CreateStore(demoRoot, true, true);
            both.Set("demo-both", RepeatedByte(8192, 90), TimeSpan.FromMinutes(60));

            StateForgeFileStore aes = CreateAesStore(demoRoot, false);
            aes.Set("demo-aes", new byte[] { 12, 13, 14, 15 }, TimeSpan.FromMinutes(60));

            StateForgeFileStore compressedAes = CreateAesStore(demoRoot, true);
            compressedAes.Set("demo-compressed-aes", RepeatedByte(8192, 88), TimeSpan.FromMinutes(60));

            StateForgeStoreDiagnostics diagnostics = compressedAes.GetDiagnostics();
            Require(diagnostics.SessionFileCount >= 6, "Demo store did not contain expected sessions.");

            StateForgeStoreStats stats = compressedAes.GetStats();
            Require(stats.AesEncryptedSessions >= 2, "Demo store did not contain expected AES encrypted sessions.");
        }


        private static StateForgeFileStore CreateAesStore(string root, bool compression)
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = root;
            options.ShardDepth = 1;
            options.EnableCompression = compression;
            options.EnableEncryption = true;
            options.ProtectionMode = StateForgeProtectionMode.Aes;
            options.AesKeyBase64 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
            options.UseWindowsDpapi = true;

            return new StateForgeFileStore(options);
        }

        private static StateForgeFileStore CreateStore(string root, bool compression, bool encryption)
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = root;
            options.ShardDepth = 1;
            options.EnableCompression = compression;
            options.EnableEncryption = encryption;
            options.UseWindowsDpapi = true;

            return new StateForgeFileStore(options);
        }

        private static SmokeResult Run(string name, Action action)
        {
            try
            {
                action();
                return new SmokeResult { Name = name, Passed = true };
            }
            catch (Exception ex)
            {
                return new SmokeResult { Name = name, Passed = false, Error = ex.GetType().Name + ": " + ex.Message };
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static byte[] RepeatedByte(int length, byte value)
        {
            byte[] buffer = new byte[length];

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = value;
            }

            return buffer;
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null)
            {
                return left == right;
            }

            if (left.Length != right.Length)
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

        private static string MakeRoot(string root, string name)
        {
            return Path.Combine(root, name);
        }

        private static string ReadOption(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
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
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch
            {
            }
        }

        private sealed class SmokeResult
        {
            public string Name { get; set; }
            public bool Passed { get; set; }
            public string Error { get; set; }
        }
    }
}
