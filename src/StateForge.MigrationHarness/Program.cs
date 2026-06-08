using System;
using System.IO;
using System.Text;
using StateForge.FileStore;

namespace StateForge.MigrationHarness
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeMigrationHarness");
                Directory.CreateDirectory(root);

                string legacyPath = Path.Combine(root, "legacy.bin");
                string stfg2Path = Path.Combine(root, "legacy.stfg2");

                File.WriteAllBytes(legacyPath, Encoding.UTF8.GetBytes("legacy-session-payload"));

                StateForgeStfg2MigrationResult migration = StateForgeStfg2Migrator.MigrateFile(
                    legacyPath,
                    stfg2Path,
                    "key-004",
                    true);

                Require(migration.Migrated, "Legacy payload was not migrated.");
                Require(!migration.SourceWasStfg2, "Legacy payload was incorrectly detected as STFG2.");
                Require(File.Exists(stfg2Path), "STFG2 destination was not created.");

                byte[] migratedBytes = File.ReadAllBytes(stfg2Path);
                StateForgeStfg2EnvelopeResult envelope = StateForgeStfg2Envelope.Unwrap(migratedBytes);

                Require(envelope.IsStfg2, "Migrated file is not STFG2.");
                Require(envelope.ChecksumValid, "Migrated checksum invalid.");
                Require(envelope.KeyId == "key-004", "Migrated KeyId mismatch.");
                Require(Encoding.UTF8.GetString(envelope.Payload) == "legacy-session-payload", "Migrated payload mismatch.");

                string copyPath = Path.Combine(root, "already.stfg2");
                StateForgeStfg2MigrationResult second = StateForgeStfg2Migrator.MigrateFile(
                    stfg2Path,
                    copyPath,
                    "key-005",
                    true);

                Require(!second.Migrated, "Existing STFG2 payload should not be re-wrapped.");
                Require(second.SourceWasStfg2, "Existing STFG2 payload was not detected.");

                Console.WriteLine("PASS: legacy payload migration");
                Console.WriteLine("PASS: STFG2 destination creation");
                Console.WriteLine("PASS: migrated KeyId");
                Console.WriteLine("PASS: migrated checksum");
                Console.WriteLine("PASS: existing STFG2 passthrough");

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
