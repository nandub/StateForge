using System;
using System.IO;
using System.Text;
using StateForge.Replication;

namespace StateForge.ReplicaCatchUpTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeReplicaCatchUpTests");
                string primary = Path.Combine(root, "primary");
                string replica = Path.Combine(root, "replica");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(Path.Combine(primary, "sessions", "aa"));
                Directory.CreateDirectory(Path.Combine(replica, "sessions", "aa"));
                Directory.CreateDirectory(Path.Combine(primary, "sessions", "bb"));
                Directory.CreateDirectory(Path.Combine(replica, "sessions", "bb"));
                Directory.CreateDirectory(Path.Combine(primary, "sessions", "cc"));
                Directory.CreateDirectory(Path.Combine(replica, "sessions", "cc"));

                WriteFixture(primary, "sessions\\aa\\same.stfg", "same");
                WriteFixture(replica, "sessions\\aa\\same.stfg", "same");

                WriteFixture(primary, "sessions\\bb\\missing.stfg", "missing");

                // Same relative path, same length, different content. This proves SHA256-based drift detection.
                WriteFixture(primary, "sessions\\cc\\changed.stfg", "aaaa");
                WriteFixture(replica, "sessions\\cc\\changed.stfg", "bbbb");

                WriteFixture(replica, "sessions\\aa\\extra.stfg", "extra");

                StateForgeReplicaCatchUpService service = new StateForgeReplicaCatchUpService();

                StateForgeReplicaCatchUpOptions dryRun = new StateForgeReplicaCatchUpOptions();
                dryRun.PrimaryRootPath = primary;
                dryRun.ReplicaRootPath = replica;
                dryRun.DryRun = true;
                dryRun.DeleteExtraReplicaFiles = true;

                StateForgeReplicaCatchUpResult plan = service.Apply(dryRun);

                Require(plan.Success, "Dry-run plan failed.");
                Require(plan.DryRun, "Plan should be dry-run.");
                Require(plan.MissingFiles == 1, "Missing count mismatch.");
                Require(plan.ChangedFiles == 1, "Changed count mismatch for equal-length different content.");
                Require(plan.ExtraFiles == 1, "Extra count mismatch.");
                Require(plan.CopiedFiles == 0, "Dry-run should not copy files.");
                Require(!File.Exists(Path.Combine(replica, "sessions", "bb", "missing.stfg")), "Dry-run copied missing file.");

                StateForgeReplicaCatchUpOptions apply = new StateForgeReplicaCatchUpOptions();
                apply.PrimaryRootPath = primary;
                apply.ReplicaRootPath = replica;
                apply.DryRun = false;
                apply.DeleteExtraReplicaFiles = true;

                StateForgeReplicaCatchUpResult result = service.Apply(apply);

                Require(result.Success, "Apply failed.");
                Require(result.CopiedFiles == 2, "Copied file count mismatch.");
                Require(result.DeletedFiles == 1, "Deleted file count mismatch.");
                Require(File.Exists(Path.Combine(replica, "sessions", "bb", "missing.stfg")), "Missing file was not copied.");
                Require(ReadFixture(replica, "sessions\\cc\\changed.stfg") == "aaaa", "Changed file was not copied from primary.");
                Require(!File.Exists(Path.Combine(replica, "sessions", "aa", "extra.stfg")), "Extra file was not deleted.");

                StateForgeReplicaCatchUpResult finalPlan = service.Plan(apply);
                Require(finalPlan.MissingFiles == 0, "Replica still missing files.");
                Require(finalPlan.ChangedFiles == 0, "Replica still has changed files.");
                Require(finalPlan.ExtraFiles == 0, "Replica still has extra files.");

                Console.WriteLine("PASS: replica catch-up dry-run");
                Console.WriteLine("PASS: missing file detection");
                Console.WriteLine("PASS: equal-length changed file detection");
                Console.WriteLine("PASS: extra file detection");
                Console.WriteLine("PASS: replica catch-up apply");
                Console.WriteLine("PASS: replica convergence");

                Directory.Delete(root, true);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                return 1;
            }
        }

        private static void WriteFixture(string root, string relativePath, string value)
        {
            string path = Path.Combine(root, relativePath);
            string directory = Path.GetDirectoryName(path);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, Encoding.UTF8.GetBytes(value));
        }

        private static string ReadFixture(string root, string relativePath)
        {
            string path = Path.Combine(root, relativePath);
            return Encoding.UTF8.GetString(File.ReadAllBytes(path));
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
