using System;
using System.IO;
using System.Text;
using StateForge.FileStore;

namespace StateForge.StoreMigrationHarness
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeStoreMigrationHarness");
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(root);

                string legacy1 = Path.Combine(root, "one.stfg");
                string legacy2 = Path.Combine(root, "nested", "two.stfg");
                Directory.CreateDirectory(Path.GetDirectoryName(legacy2));

                File.WriteAllBytes(legacy1, Encoding.UTF8.GetBytes("legacy-one"));
                File.WriteAllBytes(legacy2, Encoding.UTF8.GetBytes("legacy-two"));

                StateForgeStfg2StoreMigrationResult dry = StateForgeStfg2StoreMigrator.MigrateStore(
                    root,
                    "key-store",
                    true,
                    false,
                    "*.stfg");

                Require(dry.FilesScanned == 2, "Dry run scan count mismatch.");
                Require(dry.LegacyFilesFound == 2, "Dry run legacy count mismatch.");
                Require(dry.MigratedFiles == 0, "Dry run should not migrate.");

                StateForgeStfg2StoreMigrationResult applied = StateForgeStfg2StoreMigrator.MigrateStore(
                    root,
                    "key-store",
                    false,
                    true,
                    "*.stfg");

                Require(applied.FilesScanned == 2, "Apply scan count mismatch.");
                Require(applied.LegacyFilesFound == 2, "Apply legacy count mismatch.");
                Require(applied.MigratedFiles == 2, "Apply migrated count mismatch.");
                Require(File.Exists(legacy1 + ".stfg1.bak"), "Backup missing for one.stfg.");
                Require(File.Exists(legacy2 + ".stfg1.bak"), "Backup missing for two.stfg.");

                StateForgeStfg2EnvelopeResult one = StateForgeStfg2Envelope.Unwrap(File.ReadAllBytes(legacy1));
                StateForgeStfg2EnvelopeResult two = StateForgeStfg2Envelope.Unwrap(File.ReadAllBytes(legacy2));

                Require(one.IsStfg2, "one.stfg was not migrated.");
                Require(two.IsStfg2, "two.stfg was not migrated.");
                Require(one.KeyId == "key-store", "one.stfg KeyId mismatch.");
                Require(two.KeyId == "key-store", "two.stfg KeyId mismatch.");

                StateForgeStfg2StoreMigrationResult second = StateForgeStfg2StoreMigrator.MigrateStore(
                    root,
                    "key-store",
                    false,
                    true,
                    "*.stfg");

                Require(second.Stfg2FilesSkipped == 2, "Second pass should skip STFG2 files.");
                Require(second.MigratedFiles == 0, "Second pass should not re-migrate.");

                Console.WriteLine("PASS: store migration dry-run");
                Console.WriteLine("PASS: store migration apply");
                Console.WriteLine("PASS: store migration backups");
                Console.WriteLine("PASS: store migration KeyId");
                Console.WriteLine("PASS: store migration second-pass skip");

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
