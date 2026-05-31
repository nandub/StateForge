using System;
using System.IO;
using StateForge.Core;
using StateForge.FileStore;

namespace StateForge.FarmTests
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string root = ReadOption(args, "--root");
            string key = ReadOption(args, "--aes-key");
            bool keep = HasSwitch(args, "--keep");

            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.Combine(Path.GetTempPath(), "StateForgeFarmTests", Guid.NewGuid().ToString("N"));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                key = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
            }

            root = Path.GetFullPath(root);

            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            Console.WriteLine("StateForge Farm Simulation");
            Console.WriteLine("--------------------------");
            Console.WriteLine("RootPath: {0}", root);

            StateForgeFileStore nodeA = CreateNode(root, key);
            StateForgeFileStore nodeB = CreateNode(root, key);
            StateForgeFileStore nodeC = CreateNode(root, key);
            StateForgeFileStore nodeD = CreateNode(root, key);

            byte[] payloadA = new byte[] { 1, 2, 3 };
            byte[] payloadB = new byte[] { 4, 5, 6, 7 };

            nodeA.Set("farm-session", payloadA, TimeSpan.FromMinutes(30));
            Require(BytesEqual(payloadA, nodeB.Get("farm-session").Value), "NodeB could not read NodeA session.");

            StateForgeLockResult lockResult = nodeC.GetAndLock("farm-session", TimeSpan.FromSeconds(30));
            Require(lockResult.Found, "NodeC could not find session.");
            Require(!lockResult.LockedByOtherRequest, "NodeC could not acquire lock.");

            bool updated = nodeC.SetAndUnlock("farm-session", payloadB, TimeSpan.FromMinutes(30), lockResult.LockId);
            Require(updated, "NodeC could not update and unlock session.");

            Require(BytesEqual(payloadB, nodeD.Get("farm-session").Value), "NodeD could not read NodeC update.");

            StateForgeEntryInfo info = null;
            foreach (StateForgeEntryInfo item in nodeD.Enumerate())
            {
                if (item.Key == "farm-session")
                {
                    info = item;
                    break;
                }
            }

            Require(info != null, "Farm session was not enumerable.");
            Require(info.Compressed, "Farm session was not compressed.");
            Require(info.AesEncrypted, "Farm session was not AES encrypted.");

            StateForgeStoreStats stats = nodeD.GetStats();

            Console.WriteLine();
            Console.WriteLine("PASS: NodeA write -> NodeB read -> NodeC update -> NodeD read");
            Console.WriteLine("TotalSessions={0}", stats.TotalSessions);
            Console.WriteLine("CompressedSessions={0}", stats.CompressedSessions);
            Console.WriteLine("AesEncryptedSessions={0}", stats.AesEncryptedSessions);

            if (!keep)
            {
                Directory.Delete(root, true);
            }
            else
            {
                Console.WriteLine("Kept farm test store: {0}", root);
            }

            return 0;
        }

        private static StateForgeFileStore CreateNode(string root, string key)
        {
            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = root;
            options.ShardDepth = 1;
            options.EnableCompression = true;
            options.EnableEncryption = true;
            options.ProtectionMode = StateForgeProtectionMode.Aes;
            options.AesKeyBase64 = key;
            options.KeepBackups = false;

            return new StateForgeFileStore(options);
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (left == null || right == null) return left == right;
            if (left.Length != right.Length) return false;

            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }

            return true;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static string ReadOption(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static bool HasSwitch(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
