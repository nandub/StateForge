using System;
using System.IO;
using StateForge.Format;

namespace StateForge.FileStore
{
    /// <summary>Scans a store tree and optionally converts legacy record files to STFG2 envelopes in place.</summary>
    public static class StateForgeStfg2StoreMigrator
    {
        /// <summary>Scans matching records and optionally applies in-place STFG2 wrapping with backups.</summary>
        /// <param name="rootPath">The directory tree to scan.</param>
        /// <param name="keyId">The optional key identifier for newly wrapped legacy bytes.</param>
        /// <param name="dryRun"><see langword="true"/> to report legacy records without writing files.</param>
        /// <param name="apply"><see langword="true"/> to enable migration writes when not in dry-run mode.</param>
        /// <param name="searchPattern">The file search pattern, or a blank value to use <c>*.stfg</c>.</param>
        /// <returns>Scan, skip, migration, failure, and error details.</returns>
        /// <exception cref="ArgumentException"><paramref name="rootPath"/> is blank.</exception>
        /// <exception cref="DirectoryNotFoundException"><paramref name="rootPath"/> does not exist.</exception>
        /// <remarks>
        /// Before replacing a legacy record, the migrator creates a sibling <c>.stfg1.bak</c> file
        /// when one does not already exist. Legacy bytes are wrapped without decoding their payload.
        /// </remarks>
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
