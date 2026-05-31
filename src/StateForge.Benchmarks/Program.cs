using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using StateForge.Core;
using StateForge.FileStore;

namespace StateForge.Benchmarks
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            BenchmarkOptions options = BenchmarkOptions.Parse(args);

            if (string.IsNullOrWhiteSpace(options.RootPath))
            {
                options.RootPath = Path.Combine(Path.GetTempPath(), "StateForgeBenchmarks", Guid.NewGuid().ToString("N"));
            }

            options.RootPath = Path.GetFullPath(options.RootPath);

            if (Directory.Exists(options.RootPath) && options.Clean)
            {
                Directory.Delete(options.RootPath, true);
            }

            Directory.CreateDirectory(options.RootPath);

            Console.WriteLine("StateForge Benchmarks");
            Console.WriteLine("---------------------");
            Console.WriteLine("RootPath     : {0}", options.RootPath);
            Console.WriteLine("Sessions     : {0}", options.Sessions);
            Console.WriteLine("PayloadBytes : {0}", options.PayloadBytes);
            Console.WriteLine("Threads      : {0}", options.Threads);
            Console.WriteLine("Compression  : {0}", options.EnableCompression);
            Console.WriteLine("Encryption   : {0}", options.EnableEncryption);
            Console.WriteLine("Protection   : {0}", options.ProtectionMode);
            Console.WriteLine("KeepBackups  : {0}", options.KeepBackups);
            Console.WriteLine();

            List<BenchmarkResult> results = new List<BenchmarkResult>();

            StateForgeFileStore store = CreateStore(options);
            byte[] payload = CreatePayload(options.PayloadBytes);

            results.Add(Run("create", options.Sessions, delegate
            {
                for (int i = 0; i < options.Sessions; i++)
                {
                    store.Set("session-" + i.ToString("D8"), payload, TimeSpan.FromMinutes(60));
                }
            }));

            results.Add(Run("read sequential", options.Sessions, delegate
            {
                for (int i = 0; i < options.Sessions; i++)
                {
                    StateForgeEntry entry = store.Get("session-" + i.ToString("D8"));
                    if (entry == null)
                    {
                        throw new InvalidOperationException("Missing session during read.");
                    }
                }
            }));

            results.Add(Run("lock-update-unlock sequential", options.Sessions, delegate
            {
                for (int i = 0; i < options.Sessions; i++)
                {
                    string key = "session-" + i.ToString("D8");
                    StateForgeLockResult lockResult = store.GetAndLock(key, TimeSpan.FromSeconds(30));

                    if (!lockResult.Found || lockResult.LockedByOtherRequest)
                    {
                        throw new InvalidOperationException("Unable to acquire lock.");
                    }

                    bool updated = store.SetAndUnlock(key, payload, TimeSpan.FromMinutes(60), lockResult.LockId);

                    if (!updated)
                    {
                        throw new InvalidOperationException("Unable to update locked session.");
                    }
                }
            }));

            results.Add(Run("read concurrent", options.Sessions, delegate
            {
                RunConcurrent(options.Threads, options.Sessions, delegate(int index)
                {
                    StateForgeEntry entry = store.Get("session-" + index.ToString("D8"));
                    if (entry == null)
                    {
                        throw new InvalidOperationException("Missing session during concurrent read.");
                    }
                });
            }));

            results.Add(Run("update concurrent", options.Sessions, delegate
            {
                RunConcurrent(options.Threads, options.Sessions, delegate(int index)
                {
                    string key = "session-" + index.ToString("D8");
                    StateForgeLockResult lockResult = store.GetAndLock(key, TimeSpan.FromSeconds(30));

                    if (!lockResult.Found || lockResult.LockedByOtherRequest)
                    {
                        throw new InvalidOperationException("Unable to acquire concurrent lock.");
                    }

                    bool updated = store.SetAndUnlock(key, payload, TimeSpan.FromMinutes(60), lockResult.LockId);

                    if (!updated)
                    {
                        throw new InvalidOperationException("Unable to update concurrent locked session.");
                    }
                });
            }));

            results.Add(Run("enumerate", options.Sessions, delegate
            {
                int count = 0;
                foreach (StateForgeEntryInfo item in store.Enumerate())
                {
                    count++;
                }

                if (count < options.Sessions)
                {
                    throw new InvalidOperationException("Enumeration count was lower than expected.");
                }
            }));

            results.Add(Run("cleanup expired", options.Sessions, delegate
            {
                for (int i = 0; i < options.Sessions; i++)
                {
                    store.Set("expired-" + i.ToString("D8"), payload, TimeSpan.FromMilliseconds(1));
                }

                Thread.Sleep(50);
                StateForgeCleanupResult cleanup = store.CleanupExpired(true);

                if (cleanup.ExpiredDeleted < options.Sessions)
                {
                    throw new InvalidOperationException("Cleanup did not delete expected expired sessions.");
                }
            }));

            Console.WriteLine();
            Console.WriteLine("Benchmark Results");
            Console.WriteLine("-----------------");
            Console.WriteLine("{0,-32} {1,12} {2,14} {3,14}", "Scenario", "Operations", "ElapsedMs", "OpsPerSec");

            foreach (BenchmarkResult result in results)
            {
                Console.WriteLine("{0,-32} {1,12} {2,14:N0} {3,14:N2}",
                    result.Name,
                    result.Operations,
                    result.ElapsedMilliseconds,
                    result.OperationsPerSecond);
            }

            Console.WriteLine();
            Console.WriteLine("Diagnostics");
            Console.WriteLine("-----------");
            StateForgeStoreDiagnostics diagnostics = store.GetDiagnostics();
            Console.WriteLine("Sessions   : {0}", diagnostics.SessionFileCount);
            Console.WriteLine("Temp       : {0}", diagnostics.TempFileCount);
            Console.WriteLine("Backups    : {0}", diagnostics.BackupFileCount);
            Console.WriteLine("Quarantine : {0}", diagnostics.QuarantineFileCount);

            if (!options.Keep)
            {
                TryDeleteDirectory(options.RootPath);
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Kept benchmark store:");
                Console.WriteLine(options.RootPath);
            }

            return 0;
        }

        private static BenchmarkResult Run(string name, int operations, Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();

            double seconds = Math.Max(stopwatch.Elapsed.TotalSeconds, 0.001);

            return new BenchmarkResult
            {
                Name = name,
                Operations = operations,
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds,
                OperationsPerSecond = operations / seconds
            };
        }

        private static void RunConcurrent(int threads, int operations, Action<int> action)
        {
            if (threads < 1)
            {
                threads = 1;
            }

            int next = -1;
            Exception failure = null;
            Thread[] workers = new Thread[threads];

            for (int t = 0; t < threads; t++)
            {
                workers[t] = new Thread(delegate()
                {
                    while (true)
                    {
                        int index = Interlocked.Increment(ref next);

                        if (index >= operations)
                        {
                            return;
                        }

                        try
                        {
                            action(index);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.CompareExchange(ref failure, ex, null);
                            return;
                        }
                    }
                });

                workers[t].IsBackground = true;
                workers[t].Start();
            }

            for (int t = 0; t < workers.Length; t++)
            {
                workers[t].Join();
            }

            if (failure != null)
            {
                throw failure;
            }
        }

        private static StateForgeFileStore CreateStore(BenchmarkOptions options)
        {
            StateForgeFileStoreOptions storeOptions = new StateForgeFileStoreOptions();
            storeOptions.RootPath = options.RootPath;
            storeOptions.ShardDepth = 1;
            storeOptions.EnableCompression = options.EnableCompression;
            storeOptions.EnableEncryption = options.EnableEncryption;
            storeOptions.ProtectionMode = options.ProtectionMode;
            storeOptions.AesKeyBase64 = options.AesKeyBase64;
            storeOptions.UseWindowsDpapi = true;
            storeOptions.MutexTimeoutMilliseconds = 30000;
            storeOptions.KeepBackups = options.KeepBackups;

            return new StateForgeFileStore(storeOptions);
        }

        private static byte[] CreatePayload(int length)
        {
            if (length < 1)
            {
                length = 1;
            }

            byte[] payload = new byte[length];

            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = (byte)(i % 251);
            }

            return payload;
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

        private sealed class BenchmarkResult
        {
            public string Name { get; set; }
            public int Operations { get; set; }
            public long ElapsedMilliseconds { get; set; }
            public double OperationsPerSecond { get; set; }
        }

        private sealed class BenchmarkOptions
        {
            public string RootPath { get; set; }
            public int Sessions { get; set; }
            public int PayloadBytes { get; set; }
            public int Threads { get; set; }
            public bool EnableCompression { get; set; }
            public bool EnableEncryption { get; set; }
            public StateForgeProtectionMode ProtectionMode { get; set; }
            public string AesKeyBase64 { get; set; }
            public bool Keep { get; set; }
            public bool KeepBackups { get; set; }
            public bool Clean { get; set; }

            public BenchmarkOptions()
            {
                Sessions = 1000;
                PayloadBytes = 1024;
                Threads = Environment.ProcessorCount;
                Clean = true;
            }

            public static BenchmarkOptions Parse(string[] args)
            {
                BenchmarkOptions options = new BenchmarkOptions();

                options.RootPath = ReadOption(args, "--root");
                options.Sessions = ReadInt(args, "--sessions", options.Sessions);
                options.PayloadBytes = ReadInt(args, "--payload-bytes", options.PayloadBytes);
                options.Threads = ReadInt(args, "--threads", options.Threads);
                options.EnableCompression = HasSwitch(args, "--compression");
                options.EnableEncryption = HasSwitch(args, "--encryption") || HasSwitch(args, "--aes") || HasSwitch(args, "--dpapi");
                options.ProtectionMode = HasSwitch(args, "--aes") ? StateForgeProtectionMode.Aes : StateForgeProtectionMode.Dpapi;
                options.AesKeyBase64 = ReadOption(args, "--aes-key");

                if (HasSwitch(args, "--aes") && string.IsNullOrWhiteSpace(options.AesKeyBase64))
                {
                    options.AesKeyBase64 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
                }
                options.Keep = HasSwitch(args, "--keep");
                options.KeepBackups = HasSwitch(args, "--keep-backups");
                options.Clean = !HasSwitch(args, "--no-clean");

                return options;
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
        }
    }
}
