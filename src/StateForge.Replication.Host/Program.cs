using System;
using StateForge.Replication;

namespace StateForge.Replication.Host
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                string primary = ReadOption(args, "--primary");
                string replicas = ReadOption(args, "--replicas");
                string manifest = ReadOption(args, "--manifest");
                bool dryRun = HasSwitch(args, "--dry-run");
                bool noConflictDetection = HasSwitch(args, "--no-conflict-detection");

                if (string.IsNullOrWhiteSpace(primary))
                {
                    Console.Error.WriteLine("Missing required --primary option.");
                    return 2;
                }

                if (string.IsNullOrWhiteSpace(replicas))
                {
                    Console.Error.WriteLine("Missing required --replicas option.");
                    return 2;
                }

                StateForgeReplicationOptions options = new StateForgeReplicationOptions();
                options.PrimaryRootPath = primary;
                options.DryRun = dryRun;
                options.DetectConflicts = !noConflictDetection;
                options.ManifestPath = manifest;

                string[] replicaPaths = replicas.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < replicaPaths.Length; i++)
                {
                    options.Replicas.Add(new StateForgeReplicaNode
                    {
                        Name = "replica-" + (i + 1).ToString(),
                        RootPath = replicaPaths[i]
                    });
                }

                StateForgeReplicationResult health = StateForgeReplicationHealth.Check(options);

                if (!health.Success)
                {
                    WriteResult("health", health);
                    return 1;
                }

                StateForgeFileReplicator replicator = new StateForgeFileReplicator();
                StateForgeReplicationResult result = replicator.Replicate(options);
                WriteResult("replication", result);

                return result.Success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static void WriteResult(string phase, StateForgeReplicationResult result)
        {
            Console.WriteLine("StateForge Replication Host");
            Console.WriteLine("---------------------------");
            Console.WriteLine("Phase             : {0}", phase);
            Console.WriteLine("DryRun            : {0}", result.DryRun);
            Console.WriteLine("SourceFilesScanned: {0}", result.SourceFilesScanned);
            Console.WriteLine("ReplicasVisited   : {0}", result.ReplicasVisited);
            Console.WriteLine("FilesCopied       : {0}", result.FilesCopied);
            Console.WriteLine("FilesSkipped      : {0}", result.FilesSkipped);
            Console.WriteLine("Conflicts         : {0}", result.Conflicts);
            Console.WriteLine("Errors            : {0}", result.Errors);
            Console.WriteLine("ManifestPath      : {0}", result.ManifestPath);
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
    }
}
