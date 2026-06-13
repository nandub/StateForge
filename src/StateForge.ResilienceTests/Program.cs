using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using StateForge.Core;
using StateForge.FileStore;

namespace StateForge.ResilienceTests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string root = ReadOption(args, "--root");
            int sessions = ReadInt(args, "--sessions", 10000);
            bool keep = HasSwitch(args, "--keep");

            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.Combine(Path.GetTempPath(), "StateForgeResilience", Guid.NewGuid().ToString("N"));
            }

            root = Path.GetFullPath(root);

            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            List<TestResult> results = new List<TestResult>();

            results.Add(Run("Lock stealing after simulated crash", delegate { TestLockStealing(root); }));
            results.Add(Run("Crash-style store recreation", delegate { TestStoreRecreation(root); }));
            results.Add(Run("High session count stats", delegate { TestHighSessionCount(root, sessions); }));
            results.Add(Run("Provider-style operation sequence", delegate { TestProviderStyleSequence(root); }));

            Console.WriteLine();
            Console.WriteLine("StateForge Resilience Test Summary");
            Console.WriteLine("----------------------------------");

            foreach (TestResult result in results)
            {
                Console.WriteLine("{0}: {1}", result.Passed ? "PASS" : "FAIL", result.Name);
                if (!result.Passed)
                {
                    Console.WriteLine("      {0}", result.Error);
                }
            }

            if (keep)
            {
                Console.WriteLine();
                Console.WriteLine("Kept resilience root: {0}", root);
            }
            else
            {
                TryDeleteDirectory(root);
            }

            int failures = 0;
            foreach (TestResult result in results)
            {
                if (!result.Passed)
                {
                    failures++;
                }
            }

            return failures == 0 ? 0 : 1;
        }

        private static void TestLockStealing(string root)
        {
            string path = Path.Combine(root, "lock-stealing");
            StateForgeFileStore nodeA = CreateStore(path);
            StateForgeFileStore nodeB = CreateStore(path);

            nodeA.Set("lock-crash-session", new byte[] { 1 }, TimeSpan.FromMinutes(30));

            StateForgeLockResult nodeALock = nodeA.GetAndLock("lock-crash-session", TimeSpan.FromMinutes(30));
            Require(nodeALock.Found, "NodeA could not lock session.");
            Require(!nodeALock.LockedByOtherRequest, "NodeA lock unexpectedly blocked.");

            // Simulated crash: NodeA never calls Unlock or SetAndUnlock.
            StateForgeLockResult nodeBBlocked = nodeB.GetAndLock("lock-crash-session", TimeSpan.FromMinutes(30));
            Require(nodeBBlocked.LockedByOtherRequest, "NodeB should have been blocked before stale timeout.");

            StateForgeLockResult nodeBSteal = nodeB.GetAndLock("lock-crash-session", TimeSpan.Zero);
            Require(nodeBSteal.Found, "NodeB could not find session during stale-lock recovery.");
            Require(!nodeBSteal.LockedByOtherRequest, "NodeB could not steal stale lock.");
            Require(nodeBSteal.LockId > nodeALock.LockId, "LockId did not advance after stealing stale lock.");

            bool updated = nodeB.SetAndUnlock("lock-crash-session", new byte[] { 2 }, TimeSpan.FromMinutes(30), nodeBSteal.LockId);
            Require(updated, "NodeB could not update stolen lock.");

            bool staleUpdate = nodeA.SetAndUnlock("lock-crash-session", new byte[] { 3 }, TimeSpan.FromMinutes(30), nodeALock.LockId);
            Require(!staleUpdate, "Stale lock holder overwrote a completed stolen lock.");

            StateForgeEntry finalEntry = nodeA.Get("lock-crash-session");
            Require(finalEntry != null, "Final entry missing.");
            Require(finalEntry.Value.Length == 1 && finalEntry.Value[0] == 2, "Final value mismatch after stale-lock recovery.");
        }

        private static void TestStoreRecreation(string root)
        {
            string path = Path.Combine(root, "store-recreation");
            byte[] payload = new byte[] { 5, 6, 7, 8 };

            StateForgeFileStore before = CreateStore(path);
            before.Set("recreate-session", payload, TimeSpan.FromMinutes(30));

            // Simulated process restart: new store instance, same root.
            StateForgeFileStore after = CreateStore(path);
            StateForgeEntry entry = after.Get("recreate-session");

            Require(entry != null, "Entry missing after store recreation.");
            Require(BytesEqual(payload, entry.Value), "Payload mismatch after store recreation.");
        }

        private static void TestHighSessionCount(string root, int sessions)
        {
            string path = Path.Combine(root, "high-count");
            StateForgeFileStore store = CreateStore(path);

            byte[] payload = new byte[512];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = (byte)(i % 251);
            }

            Stopwatch watch = Stopwatch.StartNew();

            for (int i = 0; i < sessions; i++)
            {
                store.Set("bulk-" + i.ToString("D8"), payload, TimeSpan.FromMinutes(30));
            }

            watch.Stop();

            StateForgeStoreStats stats = store.GetStats();
            Require(stats.TotalSessions == sessions, "Unexpected total session count.");
            Require(stats.CompressedSessions == sessions, "Expected all sessions to be compressed.");
            Require(stats.AesEncryptedSessions == sessions, "Expected all sessions to be AES encrypted.");

            Console.WriteLine("High-count create: {0} sessions in {1:N0} ms", sessions, watch.ElapsedMilliseconds);
        }

        private static void TestProviderStyleSequence(string root)
        {
            string path = Path.Combine(root, "provider-sequence");
            StateForgeFileStore store = CreateStore(path);

            string id = "provider-session";

            // CreateUninitializedItem equivalent.
            store.Set(id, new byte[0], TimeSpan.FromMinutes(20));

            // GetItem equivalent.
            StateForgeEntry read = store.Get(id);
            Require(read != null, "Provider get failed.");

            // GetItemExclusive equivalent.
            StateForgeLockResult locked = store.GetAndLock(id, TimeSpan.FromSeconds(30));
            Require(locked.Found, "Provider exclusive get failed.");
            Require(!locked.LockedByOtherRequest, "Provider exclusive get was blocked.");

            // SetAndReleaseItemExclusive equivalent.
            bool updated = store.SetAndUnlock(id, new byte[] { 99 }, TimeSpan.FromMinutes(20), locked.LockId);
            Require(updated, "Provider set/release failed.");

            // ResetItemTimeout equivalent.
            bool refreshed = store.Refresh(id, TimeSpan.FromMinutes(40));
            Require(refreshed, "Provider refresh failed.");

            // RemoveItem equivalent.
            bool removed = store.Remove(id);
            Require(removed, "Provider remove failed.");
            Require(store.Get(id) == null, "Provider remove did not remove item.");
        }

        private static StateForgeFileStore CreateStore(string root)
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = root;
            options.ShardDepth = 1;
            options.EnableCompression = true;
            options.EnableEncryption = true;
            options.ProtectionMode = StateForgeProtectionMode.Aes;
            options.AesKeyBase64 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
            options.KeepBackups = false;
            return new StateForgeFileStore(options);
        }

        private static TestResult Run(string name, Action action)
        {
            try
            {
                action();
                return new TestResult { Name = name, Passed = true };
            }
            catch (Exception ex)
            {
                return new TestResult { Name = name, Passed = false, Error = ex.GetType().Name + ": " + ex.Message };
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null) return left == right;
            if (left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }
            return true;
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

        private static int ReadInt(string[] args, string name, int defaultValue)
        {
            string value = ReadOption(args, name);
            int parsed;
            if (int.TryParse(value, out parsed) && parsed > 0)
            {
                return parsed;
            }
            return defaultValue;
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

        private sealed class TestResult
        {
            public string Name { get; set; }
            public bool Passed { get; set; }
            public string Error { get; set; }
        }
    }
}
