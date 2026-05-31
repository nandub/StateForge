using System;
using StateForge.Core;
using StateForge.FileStore;

namespace StateForge.Maintenance
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string root = ReadOption(args, "--root");
            string once = ReadOption(args, "--once");

            if (string.IsNullOrWhiteSpace(root))
            {
                Console.Error.WriteLine("Missing required --root option.");
                return 2;
            }

            if (string.IsNullOrWhiteSpace(once))
            {
                once = "all";
            }

            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = root;

            StateForgeFileStore store = new StateForgeFileStore(options);

            if (EqualsIgnoreCase(once, "cleanup") || EqualsIgnoreCase(once, "all"))
            {
                StateForgeCleanupResult cleanup = store.CleanupExpired(true);
                Console.WriteLine("CleanupExpired={0}", cleanup.ExpiredDeleted);
                Console.WriteLine("CleanupInvalidQuarantined={0}", cleanup.InvalidQuarantined);
                Console.WriteLine("CleanupFailed={0}", cleanup.Failed);
            }

            if (EqualsIgnoreCase(once, "health") || EqualsIgnoreCase(once, "all"))
            {
                StateForgeHealthResult health = store.CheckHealth();
                Console.WriteLine("Healthy={0}", health.Healthy);
                Console.WriteLine("CanRead={0}", health.CanRead);
                Console.WriteLine("CanWrite={0}", health.CanWrite);
                Console.WriteLine("CanLock={0}", health.CanLock);
                Console.WriteLine("CanEnumerate={0}", health.CanEnumerate);
                Console.WriteLine("CanCleanup={0}", health.CanCleanup);

                foreach (string error in health.Errors)
                {
                    Console.WriteLine("HealthError={0}", error);
                }
            }

            if (EqualsIgnoreCase(once, "stats") || EqualsIgnoreCase(once, "all"))
            {
                StateForgeStoreStats stats = store.GetStats();
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

        private static bool EqualsIgnoreCase(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
