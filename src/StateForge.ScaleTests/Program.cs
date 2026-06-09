using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using StateForge.Core;
using StateForge.FileStore;
using StateForge.Prometheus;

namespace StateForge.ScaleTests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                string rootPath = ReadOption(args, "--root");
                int sessions = ReadInt(args, "--sessions", 25000);
                int payloadBytes = ReadInt(args, "--payload-bytes", 1024);
                int threads = ReadInt(args, "--threads", 8);
                bool keep = HasSwitch(args, "--keep");

                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    rootPath = Path.Combine(Path.GetTempPath(), "StateForgeScaleTests");
                }

                if (Directory.Exists(rootPath) && !keep)
                {
                    Directory.Delete(rootPath, true);
                }

                Directory.CreateDirectory(rootPath);

                StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
                options.RootPath = rootPath;

                StateForgeFileStore store = new StateForgeFileStore(options);
                byte[] payload = CreatePayload(payloadBytes);

                Console.WriteLine("StateForge Scale Tests");
                Console.WriteLine("----------------------");
                Console.WriteLine("RootPath     : {0}", rootPath);
                Console.WriteLine("Sessions     : {0}", sessions);
                Console.WriteLine("PayloadBytes : {0}", payloadBytes);
                Console.WriteLine("Threads      : {0}", threads);
                Console.WriteLine();

                long createMs = Measure(delegate
                {
                    ParallelRange(sessions, threads, delegate(int i)
                    {
                        store.Set("scale-" + i.ToString("D8"), payload, TimeSpan.FromHours(1));
                    });
                });

                long readMs = Measure(delegate
                {
                    ParallelRange(sessions, threads, delegate(int i)
                    {
                        StateForgeEntry entry = store.Get("scale-" + i.ToString("D8"));

                        if (entry == null)
                        {
                            throw new InvalidOperationException("Entry missing.");
                        }

                        byte[] entryBytes = entry.Value;

                        if (entryBytes == null)
                        {
                            throw new InvalidOperationException("Payload missing.");
                        }

                        if (entryBytes.Length != payloadBytes)
                        {
                            throw new InvalidOperationException("Payload read mismatch.");
                        }
                    });
                });

                long statsMs = Measure(delegate
                {
                    object stats = store.GetStats();
                    if (ReadIntProperty(stats, "TotalSessions") != sessions)
                    {
                        throw new InvalidOperationException("Stats session count mismatch.");
                    }
                });

                long prometheusMs = Measure(delegate
                {
                    string text = StateForgePrometheusCollector.CollectText(rootPath);
                    if (text.IndexOf("stateforge_sessions_active", StringComparison.Ordinal) < 0)
                    {
                        throw new InvalidOperationException("Prometheus session metric missing.");
                    }
                });

                long cleanupMs = Measure(delegate
                {
                    store.CleanupExpired(true);
                });

                Console.WriteLine("Scenario                 Operations      ElapsedMs      OpsPerSec");
                Print("create concurrent", sessions, createMs);
                Print("read concurrent", sessions, readMs);
                Print("stats", sessions, statsMs);
                Print("prometheus", sessions, prometheusMs);
                Print("cleanup no-expired", sessions, cleanupMs);
                Console.WriteLine();

                Console.WriteLine("PASS: scale create");
                Console.WriteLine("PASS: scale read");
                Console.WriteLine("PASS: scale stats");
                Console.WriteLine("PASS: scale prometheus");
                Console.WriteLine("PASS: scale cleanup");

                if (!keep && Directory.Exists(rootPath))
                {
                    Directory.Delete(rootPath, true);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static byte[] CreatePayload(int size)
        {
            byte[] payload = new byte[size];

            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = (byte)(i % 251);
            }

            return payload;
        }

        private static long Measure(Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }

        private static void Print(string name, int operations, long elapsedMs)
        {
            double opsPerSec = elapsedMs <= 0 ? operations : operations / (elapsedMs / 1000.0);
            Console.WriteLine("{0,-24} {1,10} {2,14} {3,14:N2}", name, operations, elapsedMs, opsPerSec);
        }

        private static void ParallelRange(int count, int threads, Action<int> action)
        {
            if (threads <= 0)
            {
                threads = 1;
            }

            int next = -1;
            Exception failure = null;
            Thread[] workers = new Thread[threads];

            for (int t = 0; t < workers.Length; t++)
            {
                workers[t] = new Thread(delegate()
                {
                    try
                    {
                        while (true)
                        {
                            int index = Interlocked.Increment(ref next);

                            if (index >= count)
                            {
                                break;
                            }

                            action(index);
                        }
                    }
                    catch (Exception ex)
                    {
                        failure = ex;
                    }
                });

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

        private static int ReadInt(string[] args, string name, int fallback)
        {
            string value = ReadOption(args, name);
            int parsed;

            if (int.TryParse(value, out parsed))
            {
                return parsed;
            }

            return fallback;
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

        private static int ReadIntProperty(object instance, string propertyName)
        {
            if (instance == null)
            {
                return 0;
            }

            object value = instance.GetType().GetProperty(propertyName).GetValue(instance, null);
            return Convert.ToInt32(value);
        }
    }
}
