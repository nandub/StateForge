using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StateForge.Core;
using StateForge.FileStore;

namespace StateForge.FileStore.Tests
{
    [TestClass]
    public sealed class StateForgeFileStoreTests
    {
        [TestMethod]
        public void Set_Then_Get_Returns_Value()
        {
            StateForgeFileStore store = CreateStore(false);
            store.Set("abc", new byte[] { 1, 2, 3 }, TimeSpan.FromMinutes(20));
            StateForgeEntry entry = store.Get("abc");

            Assert.IsNotNull(entry);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, entry.Value);
        }

        [TestMethod]
        public void Compressed_Set_Then_Get_Returns_Value()
        {
            StateForgeFileStore store = CreateStore(true);
            byte[] payload = new byte[4096];

            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = 65;
            }

            store.Set("compressed", payload, TimeSpan.FromMinutes(20));
            StateForgeEntry entry = store.Get("compressed");

            Assert.IsNotNull(entry);
            CollectionAssert.AreEqual(payload, entry.Value);
            Assert.IsTrue(store.Enumerate().First().Compressed);
        }

        [TestMethod]
        public void GetAndLock_Then_SetAndUnlock_Updates_Value()
        {
            StateForgeFileStore store = CreateStore(false);
            store.Set("abc", new byte[] { 1 }, TimeSpan.FromMinutes(20));

            StateForgeLockResult lockResult = store.GetAndLock("abc", TimeSpan.FromMinutes(5));
            Assert.IsTrue(lockResult.Found);

            bool updated = store.SetAndUnlock("abc", new byte[] { 9 }, TimeSpan.FromMinutes(20), lockResult.LockId);
            Assert.IsTrue(updated);

            StateForgeEntry entry = store.Get("abc");
            CollectionAssert.AreEqual(new byte[] { 9 }, entry.Value);
        }

        [TestMethod]
        public void Stale_Lock_Holder_Cannot_Overwrite_Completed_Stolen_Lock()
        {
            StateForgeFileStore store = CreateStore(false);
            store.Set("abc", new byte[] { 1 }, TimeSpan.FromMinutes(20));

            StateForgeLockResult first = store.GetAndLock("abc", TimeSpan.FromMinutes(5));
            StateForgeLockResult stolen = store.GetAndLock("abc", TimeSpan.Zero);

            Assert.IsTrue(store.SetAndUnlock("abc", new byte[] { 2 }, TimeSpan.FromMinutes(20), stolen.LockId));
            Assert.IsFalse(store.SetAndUnlock("abc", new byte[] { 3 }, TimeSpan.FromMinutes(20), first.LockId));
            CollectionAssert.AreEqual(new byte[] { 2 }, store.Get("abc").Value);
        }

        [TestMethod]
        public void Refresh_Does_Not_Revive_Expired_Entry()
        {
            StateForgeFileStore store = CreateStore(false);
            store.Set("expired", new byte[] { 1 }, TimeSpan.FromMilliseconds(1));
            System.Threading.Thread.Sleep(25);

            Assert.IsFalse(store.Refresh("expired", TimeSpan.FromMinutes(20)));
            Assert.IsNull(store.Get("expired"));
        }

        [TestMethod]
        public void CleanupExpired_Removes_Expired()
        {
            StateForgeFileStore store = CreateStore(false);
            store.Set("expired", new byte[] { 1 }, TimeSpan.FromMilliseconds(1));
            System.Threading.Thread.Sleep(25);

            StateForgeCleanupResult result = store.CleanupExpired(true);
            Assert.AreEqual(1, result.ExpiredDeleted);
        }

        [TestMethod]
        public void CleanupExpired_Does_Not_Quarantine_Transiently_Locked_Record()
        {
            StateForgeFileStore store = CreateStore(false);
            store.Set("active", new byte[] { 1, 2, 3 }, TimeSpan.FromMinutes(20));
            string path = store.Enumerate().Single().PhysicalPath;

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                StateForgeCleanupResult result = store.CleanupExpired(true);

                Assert.AreEqual(0, result.InvalidQuarantined);
                Assert.AreEqual(0, result.InvalidDeleted);
                Assert.AreEqual(0, result.Failed);
                Assert.IsTrue(File.Exists(path));
            }

            StateForgeEntry entry = store.Get("active");
            Assert.IsNotNull(entry);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, entry.Value);
        }

        [TestMethod]
        public void Diagnostics_Returns_Counts()
        {
            StateForgeFileStore store = CreateStore(false);
            store.Set("one", new byte[] { 1 }, TimeSpan.FromMinutes(5));

            StateForgeStoreDiagnostics diagnostics = store.GetDiagnostics();
            Assert.AreEqual(1, diagnostics.SessionFileCount);
        }

                [TestMethod]
        public void Encrypted_Set_Then_Get_Returns_Value()
        {
            StateForgeFileStore store = CreateStore(true, true);
            byte[] payload = new byte[] { 10, 20, 30, 40 };

            store.Set("encrypted", payload, TimeSpan.FromMinutes(20));
            StateForgeEntry entry = store.Get("encrypted");

            Assert.IsNotNull(entry);
            CollectionAssert.AreEqual(payload, entry.Value);
            Assert.IsTrue(store.Enumerate().First().Encrypted);
        }


        [TestMethod]
        public void Compression_And_Encryption_RoundTrips()
        {
            StateForgeFileStore store = CreateStore(true, true);
            byte[] payload = new byte[8192];

            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = 90;
            }

            store.Set("both", payload, TimeSpan.FromMinutes(20));
            StateForgeEntry entry = store.Get("both");

            Assert.IsNotNull(entry);
            CollectionAssert.AreEqual(payload, entry.Value);

            StateForgeEntryInfo info = store.Enumerate().First();
            Assert.IsTrue(info.Compressed);
            Assert.IsTrue(info.Encrypted);
        }

        private static StateForgeFileStore CreateStore(bool enableCompression)
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = Path.Combine(Path.GetTempPath(), "StateForgeTests", Guid.NewGuid().ToString("N"));
            options.ShardDepth = 1;
            options.EnableCompression = enableCompression;
            return new StateForgeFileStore(options);
        }

        private static StateForgeFileStore CreateStore(bool enableCompression, bool enableEncryption)
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = Path.Combine(Path.GetTempPath(), "StateForgeTests", Guid.NewGuid().ToString("N"));
            options.ShardDepth = 1;
            options.EnableCompression = enableCompression;
            options.EnableEncryption = enableEncryption;
            return new StateForgeFileStore(options);
        }
    }
}
