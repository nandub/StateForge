using System;
using System.Text;
using StateForge.Format;

namespace StateForge.FormatTests
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                byte[] payload = Encoding.UTF8.GetBytes("hello-stateforge");
                byte[] fileBytes = StateForgeStfg2.Write(
                    payload,
                    StateForgeFormatFlags.Compressed | StateForgeFormatFlags.Encrypted | StateForgeFormatFlags.Aes,
                    "key-002");

                Require(StateForgeStfg2.IsStfg2(fileBytes), "IsStfg2 failed.");

                StateForgeStfg2ReadResult result = StateForgeStfg2.Read(fileBytes);

                Require(result.Version == 2, "Version mismatch.");
                Require(result.KeyId == "key-002", "KeyId mismatch.");
                Require((result.Flags & StateForgeFormatFlags.Aes) == StateForgeFormatFlags.Aes, "AES flag missing.");
                Require(result.ChecksumValid, "Checksum validation failed.");
                Require(Encoding.UTF8.GetString(result.Payload) == "hello-stateforge", "Payload mismatch.");

                fileBytes[fileBytes.Length - 1] = (byte)'X';
                StateForgeStfg2ReadResult corrupted = StateForgeStfg2.Read(fileBytes);
                Require(!corrupted.ChecksumValid, "Corruption was not detected.");

                Console.WriteLine("PASS: STFG2 write");
                Console.WriteLine("PASS: STFG2 read");
                Console.WriteLine("PASS: STFG2 KeyId");
                Console.WriteLine("PASS: STFG2 checksum");
                Console.WriteLine("PASS: STFG2 corruption detection");

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
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
