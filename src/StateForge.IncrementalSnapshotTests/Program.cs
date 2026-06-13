using System;
using System.IO;
using System.Text;
using StateForge.FileStore;
using StateForge.Snapshots;

namespace StateForge.IncrementalSnapshotTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string root = Path.Combine(Path.GetTempPath(), "StateForgeIncrementalSnapshotTests");
                string primary = Path.Combine(root, "primary");
                string repository = Path.Combine(root, "repository");
                string restore = Path.Combine(root, "restore");

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }

                Directory.CreateDirectory(primary);

                StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
                options.RootPath = primary;
                options.ShardDepth = 1;

                StateForgeFileStore store = new StateForgeFileStore(options);
                byte[] basePayload = Encoding.UTF8.GetBytes("base");
                byte[] modifiedPayload = Encoding.UTF8.GetBytes("modified");

                store.Set("keep", basePayload, TimeSpan.FromMinutes(10));
                store.Set("modify", basePayload, TimeSpan.FromMinutes(10));
                store.Set("delete", basePayload, TimeSpan.FromMinutes(10));

                StateForgeIncrementalSnapshotService incremental = new StateForgeIncrementalSnapshotService();
                StateForgeSnapshotOptions baseOptions = new StateForgeSnapshotOptions();
                baseOptions.SourceRootPath = primary;
                baseOptions.SnapshotRepositoryPath = repository;
                baseOptions.SnapshotName = "base";
                baseOptions.OverwriteExisting = true;

                StateForgeSnapshotResult baseResult = incremental.CreateBase(baseOptions);
                Require(baseResult.Success, "Base snapshot failed.");
                Require(baseResult.FilesCopied == 3, "Base snapshot file count mismatch.");

                store.Set("modify", modifiedPayload, TimeSpan.FromMinutes(10));
                store.Remove("delete");
                store.Set("add", basePayload, TimeSpan.FromMinutes(10));

                StateForgeIncrementalSnapshotOptions incOptions = new StateForgeIncrementalSnapshotOptions();
                incOptions.SourceRootPath = primary;
                incOptions.SnapshotRepositoryPath = repository;
                incOptions.ParentSnapshotName = "base";
                incOptions.SnapshotName = "inc1";
                incOptions.OverwriteExisting = true;

                StateForgeIncrementalSnapshotResult incResult = incremental.CreateIncremental(incOptions);
                Require(incResult.Success, "Incremental snapshot failed.");
                Require(incResult.FilesAdded == 1, "Incremental added count mismatch.");
                Require(incResult.FilesModified == 1, "Incremental modified count mismatch.");
                Require(incResult.FilesDeleted == 1, "Incremental deleted count mismatch.");
                Require(File.Exists(incResult.ManifestPath), "Incremental manifest missing.");

                StateForgeSnapshotResult restoreResult = incremental.RestoreChain(repository, "base", new string[] { "inc1" }, restore);
                Require(restoreResult.Success, "Incremental restore chain failed.");

                StateForgeFileStoreOptions restoredOptions = new StateForgeFileStoreOptions();
                restoredOptions.RootPath = restore;
                restoredOptions.ShardDepth = 1;
                StateForgeFileStore restoredStore = new StateForgeFileStore(restoredOptions);

                Require(restoredStore.Get("keep") != null, "Keep entry missing after restore.");
                Require(restoredStore.Get("modify") != null, "Modified entry missing after restore.");
                Require(restoredStore.Get("add") != null, "Added entry missing after restore.");
                Require(restoredStore.Get("delete") == null, "Deleted entry restored incorrectly.");

                StateForgeIncrementalSnapshotManifest manifest = StateForgeIncrementalSnapshotService.ReadManifest(incResult.ManifestPath);
                Require(manifest.Entries.Count == 3, "Manifest entry count mismatch.");

                Console.WriteLine("PASS: base snapshot");
                Console.WriteLine("PASS: delta snapshot");
                Console.WriteLine("PASS: incremental restore");
                Console.WriteLine("PASS: deleted file replay");
                Console.WriteLine("PASS: manifest chain");

                Directory.Delete(root, true);
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
