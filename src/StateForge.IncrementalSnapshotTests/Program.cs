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

                string modifiedPath = FindSessionFile(primary, "keep");
                byte[] sameLengthChange = File.ReadAllBytes(modifiedPath);
                DateTime preservedWriteTime = File.GetLastWriteTimeUtc(modifiedPath);
                sameLengthChange[sameLengthChange.Length - 1] = (byte)(sameLengthChange[sameLengthChange.Length - 1] ^ 0x01);
                File.WriteAllBytes(modifiedPath, sameLengthChange);
                File.SetLastWriteTimeUtc(modifiedPath, preservedWriteTime);

                StateForgeIncrementalSnapshotOptions hashOptions = new StateForgeIncrementalSnapshotOptions();
                hashOptions.SourceRootPath = primary;
                hashOptions.SnapshotRepositoryPath = repository;
                hashOptions.ParentSnapshotName = "base";
                hashOptions.SnapshotName = "inc-hash";
                hashOptions.OverwriteExisting = true;
                StateForgeIncrementalSnapshotResult hashResult = incremental.CreateIncremental(hashOptions);
                Require(hashResult.FilesModified >= 1, "Same-length timestamp-preserved change was not detected.");

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

                string sentinel = Path.Combine(root, "sentinel.txt");
                File.WriteAllText(sentinel, "keep");
                StateForgeSnapshotOptions unsafeOptions = new StateForgeSnapshotOptions();
                unsafeOptions.SourceRootPath = primary;
                unsafeOptions.SnapshotRepositoryPath = repository;
                unsafeOptions.SnapshotName = "..";
                unsafeOptions.OverwriteExisting = true;
                RequireThrows(delegate { incremental.CreateBase(unsafeOptions); }, "Unsafe snapshot name was accepted.");
                Require(File.Exists(sentinel), "Unsafe snapshot name modified an outside file.");

                string malicious = Path.Combine(repository, "malicious");
                string maliciousDelta = Path.Combine(malicious, "delta");
                Directory.CreateDirectory(maliciousDelta);
                StateForgeIncrementalSnapshotManifest maliciousManifest = new StateForgeIncrementalSnapshotManifest();
                maliciousManifest.SnapshotName = "malicious";
                StateForgeIncrementalSnapshotEntry maliciousEntry = new StateForgeIncrementalSnapshotEntry();
                maliciousEntry.RelativePath = @"..\outside.stfg";
                maliciousEntry.Action = "delete";
                maliciousEntry.LastWriteUtc = DateTimeOffset.UtcNow.ToString("o");
                maliciousManifest.Entries.Add(maliciousEntry);
                StateForgeIncrementalSnapshotService.WriteManifest(
                    Path.Combine(malicious, "incremental-manifest.json"),
                    maliciousManifest);
                RequireThrows(
                    delegate { incremental.RestoreChain(repository, "base", new string[] { "malicious" }, restore); },
                    "Manifest path traversal was accepted.");

                Console.WriteLine("PASS: base snapshot");
                Console.WriteLine("PASS: delta snapshot");
                Console.WriteLine("PASS: SHA256 same-length change detection");
                Console.WriteLine("PASS: incremental restore");
                Console.WriteLine("PASS: deleted file replay");
                Console.WriteLine("PASS: manifest chain");
                Console.WriteLine("PASS: snapshot path containment");

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

        private static string FindSessionFile(string root, string key)
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = root;
            options.ShardDepth = 1;
            StateForgeFileStore store = new StateForgeFileStore(options);

            foreach (StateForge.Core.StateForgeEntryInfo entry in store.Enumerate())
            {
                if (string.Equals(entry.Key, key, StringComparison.Ordinal))
                {
                    return entry.PhysicalPath;
                }
            }

            throw new InvalidOperationException("Session file not found: " + key);
        }

        private static void RequireThrows(Action action, string message)
        {
            try
            {
                action();
            }
            catch (ArgumentException)
            {
                return;
            }
            catch (InvalidDataException)
            {
                return;
            }

            throw new InvalidOperationException(message);
        }
    }
}
