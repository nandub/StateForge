using System;
using System.IO;
using StateForge.Format;

namespace StateForge.FileStore
{
    public static class StateForgeStfg2StoreMigrator
    {
        public static StateForgeStfg2StoreMigrationResult MigrateStore(
            string rootPath,
            string keyId,
            bool dryRun,
            bool apply,
            string searchPattern)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Root path is required.", "rootPath");
            }

            if (!Directory.Exists(rootPath))
            {
                throw new DirectoryNotFoundException(rootPath);
            }

            if (string.IsNullOrWhiteSpace(searchPattern))
            {
                searchPattern = "*.stfg";
            }

            StateForgeStfg2StoreMigrationResult result = new StateForgeStfg2StoreMigrationResult();
            result.RootPath = rootPath;
            result.DryRun = dryRun;
            result.Applied = apply;

            string[] files = Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories);

            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                result.FilesScanned++;

                try
                {
                    byte[] bytes = File.ReadAllBytes(file);

                    if (StateForgeStfg2.IsStfg2(bytes))
                    {
                        result.Stfg2FilesSkipped++;
                        continue;
                    }

                    result.LegacyFilesFound++;

                    if (dryRun || !apply)
                    {
                        continue;
                    }

                    string backupPath = file + ".stfg1.bak";

                    if (!File.Exists(backupPath))
                    {
                        File.Copy(file, backupPath, false);
                    }

                    byte[] migrated = StateForgeStfg2Envelope.Wrap(
                        bytes,
                        false,
                        false,
                        false,
                        false,
                        keyId);

                    File.WriteAllBytes(file, migrated);
                    result.MigratedFiles++;
                }
                catch (Exception ex)
                {
                    result.FailedFiles++;
                    result.Errors.Add(file + ": " + ex.GetType().Name + ": " + ex.Message);
                }
            }

            return result;
        }
    }
}
