using System.Collections.Generic;
using System.IO;

namespace StateForge.Maintenance.Host
{
    internal static class StateForgeMaintenanceHostConfigValidator
    {
        public static List<string> Validate(StateForgeMaintenanceHostConfig config)
        {
            List<string> errors = new List<string>();

            if (config == null)
            {
                errors.Add("Config is null.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(config.RootPath))
            {
                errors.Add("RootPath is required.");
            }
            else if (!Directory.Exists(config.RootPath))
            {
                errors.Add("RootPath does not exist: " + config.RootPath);
            }

            if (config.IntervalSeconds <= 0)
            {
                errors.Add("IntervalSeconds must be greater than zero.");
            }

            if (config.MaxLogSizeMb <= 0)
            {
                errors.Add("MaxLogSizeMb must be greater than zero.");
            }

            if (config.MaxLogFiles < 0)
            {
                errors.Add("MaxLogFiles cannot be negative.");
            }

            if (config.Stfg2MigrationEnabled && !config.Stfg2MigrationDryRun && !config.Stfg2MigrationApply)
            {
                errors.Add("STFG2 migration requires dry-run or apply mode.");
            }

            if (config.Stfg2MigrationEnabled && string.IsNullOrWhiteSpace(config.Stfg2MigrationKeyId))
            {
                errors.Add("STFG2 migration requires Stfg2MigrationKeyId.");
            }

            return errors;
        }
    }
}
