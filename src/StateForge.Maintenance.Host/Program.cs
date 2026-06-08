using System;
using System.Threading;

namespace StateForge.Maintenance.Host
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                string configPath = ReadOption(args, "--config");
                StateForgeMaintenanceHostConfig config = StateForgeMaintenanceHostConfig.Load(configPath);

                string root = ReadOption(args, "--root");
                string logPath = ReadOption(args, "--log");
                string interval = ReadOption(args, "--interval-seconds");

                if (!string.IsNullOrWhiteSpace(root)) config.RootPath = root;
                if (!string.IsNullOrWhiteSpace(logPath)) config.LogPath = logPath;

                int intervalSeconds;
                if (int.TryParse(interval, out intervalSeconds) && intervalSeconds > 0)
                {
                    config.IntervalSeconds = intervalSeconds;
                }

                if (HasSwitch(args, "--json")) config.Json = true;

                if (HasSwitch(args, "--cleanup-only"))
                {
                    config.CleanupEnabled = true;
                    config.HealthEnabled = false;
                    config.StatsEnabled = false;
                    config.Stfg2MigrationEnabled = false;
                }

                if (HasSwitch(args, "--health-only"))
                {
                    config.CleanupEnabled = false;
                    config.HealthEnabled = true;
                    config.StatsEnabled = false;
                    config.Stfg2MigrationEnabled = false;
                }

                if (HasSwitch(args, "--stats-only"))
                {
                    config.CleanupEnabled = false;
                    config.HealthEnabled = false;
                    config.StatsEnabled = true;
                    config.Stfg2MigrationEnabled = false;
                }

                if (HasSwitch(args, "--migration-only"))
                {
                    config.CleanupEnabled = false;
                    config.HealthEnabled = false;
                    config.StatsEnabled = false;
                    config.Stfg2MigrationEnabled = true;
                }

                if (HasSwitch(args, "--validate-config"))
                {
                    System.Collections.Generic.List<string> errors = StateForgeMaintenanceHostConfigValidator.Validate(config);

                    if (errors.Count == 0)
                    {
                        Console.WriteLine("Success=True");
                        return 0;
                    }

                    Console.WriteLine("Success=False");

                    for (int i = 0; i < errors.Count; i++)
                    {
                        Console.WriteLine("Error={0}", errors[i]);
                    }

                    return 40;
                }

                bool once = HasSwitch(args, "--once");
                bool loop = HasSwitch(args, "--loop");

                if (!once && !loop) once = true;

                if (once)
                {
                    StateForgeMaintenanceHostResult result = StateForgeMaintenanceHostRunner.RunOnce(config);
                    StateForgeMaintenanceHostOutput.Write(result, config.Json, config.LogPath);
                    return result.Success ? 0 : 1;
                }

                while (true)
                {
                    StateForgeMaintenanceHostResult result = StateForgeMaintenanceHostRunner.RunOnce(config);
                    StateForgeMaintenanceHostOutput.Write(result, config.Json, config.LogPath);
                    Thread.Sleep(TimeSpan.FromSeconds(config.IntervalSeconds));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static string ReadOption(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (EqualsIgnoreCase(args[i], name)) return args[i + 1];
            }
            return null;
        }

        private static bool HasSwitch(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (EqualsIgnoreCase(args[i], name)) return true;
            }
            return false;
        }

        private static bool EqualsIgnoreCase(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
