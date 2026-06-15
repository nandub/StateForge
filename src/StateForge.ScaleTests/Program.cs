using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using StateForge.Core;
using StateForge.FileStore;
using StateForge.Prometheus;
using StateForge.Replication;
using StateForge.Snapshots;

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
                string exportJson = ReadOption(args, "--export-json");
                string exportCsv = ReadOption(args, "--export-csv");
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
                List<BenchmarkScenarioResult> results = new List<BenchmarkScenarioResult>();
                long managedMemoryBefore = GC.GetTotalMemory(true);

                Console.WriteLine("StateForge Scale Tests");
                Console.WriteLine("----------------------");
                Console.WriteLine("RootPath     : {0}", rootPath);
                Console.WriteLine("Sessions     : {0}", sessions);
                Console.WriteLine("PayloadBytes : {0}", payloadBytes);
                Console.WriteLine("Threads      : {0}", threads);
                Console.WriteLine();

                results.Add(RunScenario("create concurrent", sessions, delegate(BenchmarkScenarioResult result)
                {
                    ParallelRange(sessions, threads, delegate(int i)
                    {
                        long elapsed = MeasureOne(delegate()
                        {
                            store.Set("scale-" + i.ToString("D8"), payload, TimeSpan.FromHours(1));
                        });

                        result.RecordLatency(elapsed);
                    });
                }));

                results.Add(RunScenario("read concurrent", sessions, delegate(BenchmarkScenarioResult result)
                {
                    ParallelRange(sessions, threads, delegate(int i)
                    {
                        long elapsed = MeasureOne(delegate()
                        {
                            StateForgeEntry entry = store.Get("scale-" + i.ToString("D8"));

                            if (entry == null)
                            {
                                throw new InvalidOperationException("Entry missing.");
                            }

                            byte[] entryBytes = ReadEntryBytes(entry);

                            if (entryBytes == null)
                            {
                                throw new InvalidOperationException("Payload missing.");
                            }

                            if (entryBytes.Length != payloadBytes)
                            {
                                throw new InvalidOperationException("Payload read mismatch.");
                            }
                        });

                        result.RecordLatency(elapsed);
                    });
                }));

                results.Add(RunScenario("lock-update concurrent", sessions, delegate(BenchmarkScenarioResult result)
                {
                    ParallelRange(sessions, threads, delegate(int i)
                    {
                        long elapsed = MeasureOne(delegate()
                        {
                            string key = "scale-" + i.ToString("D8");
                            StateForgeLockResult lockResult = store.GetAndLock(key, TimeSpan.FromSeconds(30));
                            if (!lockResult.Found || lockResult.LockedByOtherRequest ||
                                !store.SetAndUnlock(key, payload, TimeSpan.FromHours(1), lockResult.LockId))
                            {
                                throw new InvalidOperationException("Lock-update failed.");
                            }
                        });

                        result.RecordLatency(elapsed);
                    });
                }));

                results.Add(RunScenario("refresh concurrent", sessions, delegate(BenchmarkScenarioResult result)
                {
                    ParallelRange(sessions, threads, delegate(int i)
                    {
                        long elapsed = MeasureOne(delegate()
                        {
                            if (!store.Refresh("scale-" + i.ToString("D8"), TimeSpan.FromHours(1)))
                            {
                                throw new InvalidOperationException("Refresh failed.");
                            }
                        });

                        result.RecordLatency(elapsed);
                    });
                }));

                results.Add(RunScenario("stats", sessions, delegate(BenchmarkScenarioResult result)
                {
                    long elapsed = MeasureOne(delegate()
                    {
                        object stats = store.GetStats();
                        if (ReadIntProperty(stats, "TotalSessions") != sessions)
                        {
                            throw new InvalidOperationException("Stats session count mismatch.");
                        }
                    });

                    result.RecordLatency(elapsed);
                }));

                results.Add(RunScenario("prometheus", sessions, delegate(BenchmarkScenarioResult result)
                {
                    long elapsed = MeasureOne(delegate()
                    {
                        string text = StateForgePrometheusCollector.CollectText(rootPath);
                        if (text.IndexOf("stateforge_sessions_active", StringComparison.Ordinal) < 0)
                        {
                            throw new InvalidOperationException("Prometheus session metric missing.");
                        }
                    });

                    result.RecordLatency(elapsed);
                }));

                results.Add(RunScenario("cleanup no-expired", sessions, delegate(BenchmarkScenarioResult result)
                {
                    long elapsed = MeasureOne(delegate()
                    {
                        store.CleanupExpired(true);
                    });

                    result.RecordLatency(elapsed);
                }));

                string replicaRoot = rootPath + "-replica";
                results.Add(RunScenario("replication full", sessions, delegate(BenchmarkScenarioResult result)
                {
                    if (Directory.Exists(replicaRoot))
                    {
                        Directory.Delete(replicaRoot, true);
                    }

                    StateForgeReplicationOptions replicationOptions = new StateForgeReplicationOptions();
                    replicationOptions.PrimaryRootPath = rootPath;
                    replicationOptions.Replicas.Add(new StateForgeReplicaNode
                    {
                        Name = "benchmark-replica",
                        RootPath = replicaRoot
                    });

                    long elapsed = MeasureOne(delegate()
                    {
                        StateForgeReplicationResult replication =
                            new StateForgeFileReplicator().Replicate(replicationOptions);
                        if (!replication.Success || replication.FilesCopied < sessions)
                        {
                            throw new InvalidOperationException("Replication benchmark failed.");
                        }
                    });

                    result.RecordLatency(elapsed);
                }));

                string snapshotRepository = rootPath + "-snapshots";
                results.Add(RunScenario("snapshot full", sessions, delegate(BenchmarkScenarioResult result)
                {
                    if (Directory.Exists(snapshotRepository))
                    {
                        Directory.Delete(snapshotRepository, true);
                    }

                    StateForgeSnapshotOptions snapshotOptions = new StateForgeSnapshotOptions();
                    snapshotOptions.SourceRootPath = rootPath;
                    snapshotOptions.SnapshotRepositoryPath = snapshotRepository;
                    snapshotOptions.SnapshotName = "baseline";
                    snapshotOptions.OverwriteExisting = true;

                    long elapsed = MeasureOne(delegate()
                    {
                        StateForgeSnapshotResult snapshot =
                            new StateForgeSnapshotService().Create(snapshotOptions);
                        if (!snapshot.Success || snapshot.FilesCopied < sessions)
                        {
                            throw new InvalidOperationException("Snapshot benchmark failed.");
                        }
                    });

                    result.RecordLatency(elapsed);
                }));

                long storeBytes = GetDirectoryBytes(rootPath);
                long managedMemoryBytes = Math.Max(0, GC.GetTotalMemory(true) - managedMemoryBefore);

                Console.WriteLine("Scenario                 Operations      ElapsedMs      OpsPerSec       P50ms       P95ms       P99ms");
                for (int i = 0; i < results.Count; i++)
                {
                    Print(results[i]);
                }

                Console.WriteLine();

                if (!string.IsNullOrWhiteSpace(exportJson))
                {
                    WriteJson(exportJson, rootPath, sessions, payloadBytes, threads, storeBytes, managedMemoryBytes, results);
                    Console.WriteLine("JSON export: {0}", exportJson);
                }

                if (!string.IsNullOrWhiteSpace(exportCsv))
                {
                    WriteCsv(exportCsv, storeBytes, managedMemoryBytes, results);
                    Console.WriteLine("CSV export : {0}", exportCsv);
                }

                Console.WriteLine("PASS: scale create");
                Console.WriteLine("PASS: scale read");
                Console.WriteLine("PASS: scale lock-update");
                Console.WriteLine("PASS: scale refresh");
                Console.WriteLine("PASS: scale stats");
                Console.WriteLine("PASS: scale prometheus");
                Console.WriteLine("PASS: scale cleanup");
                Console.WriteLine("PASS: scale replication");
                Console.WriteLine("PASS: scale snapshot");
                Console.WriteLine("PASS: benchmark latency percentiles");

                if (!keep && Directory.Exists(rootPath))
                {
                    Directory.Delete(rootPath, true);
                }
                if (!keep && Directory.Exists(replicaRoot))
                {
                    Directory.Delete(replicaRoot, true);
                }
                if (!keep && Directory.Exists(snapshotRepository))
                {
                    Directory.Delete(snapshotRepository, true);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static BenchmarkScenarioResult RunScenario(string name, int operations, Action<BenchmarkScenarioResult> action)
        {
            BenchmarkScenarioResult result = new BenchmarkScenarioResult();
            result.Name = name;
            result.Operations = operations;

            Stopwatch stopwatch = Stopwatch.StartNew();
            action(result);
            stopwatch.Stop();

            result.ElapsedMs = stopwatch.ElapsedMilliseconds;
            result.Calculate();
            return result;
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

        private static long MeasureOne(Action action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return stopwatch.ElapsedTicks;
        }

        private static void Print(BenchmarkScenarioResult result)
        {
            Console.WriteLine(
                "{0,-24} {1,10} {2,14} {3,14:N2} {4,10:N3} {5,10:N3} {6,10:N3}",
                result.Name,
                result.Operations,
                result.ElapsedMs,
                result.OpsPerSecond,
                result.P50Ms,
                result.P95Ms,
                result.P99Ms);
        }

        private static void ParallelRange(int count, int threads, Action<int> action)
        {
            if (threads <= 0)
            {
                threads = 1;
            }

            int next = -1;
            Exception failure = null;
            object failureLock = new object();
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
                        lock (failureLock)
                        {
                            if (failure == null)
                            {
                                failure = ex;
                            }
                        }
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

        private static byte[] ReadEntryBytes(StateForgeEntry entry)
        {
            if (entry == null)
            {
                return null;
            }

            string[] propertyNames = new string[] { "Value", "Data", "Bytes", "Content", "Body", "Buffer" };

            for (int i = 0; i < propertyNames.Length; i++)
            {
                PropertyInfo property = entry.GetType().GetProperty(propertyNames[i]);

                if (property == null)
                {
                    continue;
                }

                object value = property.GetValue(entry, null);

                if (value is byte[])
                {
                    return (byte[])value;
                }
            }

            return null;
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

        private static long GetDirectoryBytes(string path)
        {
            long total = 0;
            if (!Directory.Exists(path))
            {
                return total;
            }

            string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++)
            {
                total += new FileInfo(files[i]).Length;
            }

            return total;
        }

        private static void WriteJson(string path, string rootPath, int sessions, int payloadBytes, int threads, long storeBytes, long managedMemoryBytes, List<BenchmarkScenarioResult> results)
        {
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("{");
            builder.AppendLine("  \"version\": \"1\",");
            builder.AppendLine("  \"capturedUtc\": \"" + DateTimeOffset.UtcNow.ToString("o") + "\",");
            builder.AppendLine("  \"rootPath\": \"" + Escape(new DirectoryInfo(rootPath).Name) + "\",");
            builder.AppendLine("  \"sessions\": " + sessions.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"payloadBytes\": " + payloadBytes.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"threads\": " + threads.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"storeBytes\": " + storeBytes.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"managedMemoryBytes\": " + managedMemoryBytes.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("  \"scenarios\": [");

            for (int i = 0; i < results.Count; i++)
            {
                BenchmarkScenarioResult result = results[i];
                builder.AppendLine("    {");
                builder.AppendLine("      \"name\": \"" + Escape(result.Name) + "\",");
                builder.AppendLine("      \"operations\": " + result.Operations.ToString(CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"elapsedMs\": " + result.ElapsedMs.ToString(CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"opsPerSecond\": " + result.OpsPerSecond.ToString("0.###", CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"p50Ms\": " + result.P50Ms.ToString("0.###", CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"p95Ms\": " + result.P95Ms.ToString("0.###", CultureInfo.InvariantCulture) + ",");
                builder.AppendLine("      \"p99Ms\": " + result.P99Ms.ToString("0.###", CultureInfo.InvariantCulture));
                builder.Append("    }");

                if (i < results.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            builder.AppendLine("  ]");
            builder.AppendLine("}");

            File.WriteAllText(
                fullPath,
                builder.ToString().Replace("\r\n", "\n"),
                new UTF8Encoding(false));
        }

        private static void WriteCsv(string path, long storeBytes, long managedMemoryBytes, List<BenchmarkScenarioResult> results)
        {
            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("name,operations,elapsedMs,opsPerSecond,p50Ms,p95Ms,p99Ms,storeBytes,managedMemoryBytes");

            for (int i = 0; i < results.Count; i++)
            {
                BenchmarkScenarioResult result = results[i];
                builder.Append(EscapeCsv(result.Name)).Append(",");
                builder.Append(result.Operations.ToString(CultureInfo.InvariantCulture)).Append(",");
                builder.Append(result.ElapsedMs.ToString(CultureInfo.InvariantCulture)).Append(",");
                builder.Append(result.OpsPerSecond.ToString("0.###", CultureInfo.InvariantCulture)).Append(",");
                builder.Append(result.P50Ms.ToString("0.###", CultureInfo.InvariantCulture)).Append(",");
                builder.Append(result.P95Ms.ToString("0.###", CultureInfo.InvariantCulture)).Append(",");
                builder.Append(result.P99Ms.ToString("0.###", CultureInfo.InvariantCulture)).Append(",");
                builder.Append(storeBytes.ToString(CultureInfo.InvariantCulture)).Append(",");
                builder.Append(managedMemoryBytes.ToString(CultureInfo.InvariantCulture)).AppendLine();
            }

            File.WriteAllText(
                fullPath,
                builder.ToString().Replace("\r\n", "\n"),
                new UTF8Encoding(false));
        }

        private static string Escape(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }

    internal sealed class BenchmarkScenarioResult
    {
        private readonly List<long> latencyTicks;

        public BenchmarkScenarioResult()
        {
            latencyTicks = new List<long>();
        }

        public string Name { get; set; }

        public int Operations { get; set; }

        public long ElapsedMs { get; set; }

        public double OpsPerSecond { get; private set; }

        public double P50Ms { get; private set; }

        public double P95Ms { get; private set; }

        public double P99Ms { get; private set; }

        public void RecordLatency(long elapsedTicks)
        {
            lock (latencyTicks)
            {
                latencyTicks.Add(elapsedTicks);
            }
        }

        public void Calculate()
        {
            if (ElapsedMs <= 0)
            {
                OpsPerSecond = Operations;
            }
            else
            {
                OpsPerSecond = Operations / (ElapsedMs / 1000.0);
            }

            long[] values;

            lock (latencyTicks)
            {
                values = latencyTicks.ToArray();
            }

            Array.Sort(values);

            P50Ms = PercentileMs(values, 0.50);
            P95Ms = PercentileMs(values, 0.95);
            P99Ms = PercentileMs(values, 0.99);
        }

        private static double PercentileMs(long[] values, double percentile)
        {
            if (values == null || values.Length == 0)
            {
                return 0;
            }

            int index = (int)Math.Ceiling(values.Length * percentile) - 1;

            if (index < 0)
            {
                index = 0;
            }

            if (index >= values.Length)
            {
                index = values.Length - 1;
            }

            return values[index] * 1000.0 / Stopwatch.Frequency;
        }
    }
}
