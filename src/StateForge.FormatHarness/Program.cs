using System;
using System.Text;
using StateForge.FileStore;

namespace StateForge.FormatHarness
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                byte[] payload = Encoding.UTF8.GetBytes("stateforge-envelope");
                byte[] wrapped = StateForgeStfg2Envelope.Wrap(
                    payload,
                    true,
                    true,
                    true,
                    false,
                    "key-003");

                StateForgeStfg2EnvelopeResult result = StateForgeStfg2Envelope.Unwrap(wrapped);

                Require(result.IsStfg2, "Envelope was not STFG2.");
                Require(result.ChecksumValid, "Checksum failed.");
                Require(result.KeyId == "key-003", "KeyId mismatch.");
                Require(result.Flags.IndexOf("Compressed", StringComparison.OrdinalIgnoreCase) >= 0, "Compressed flag missing.");
                Require(result.Flags.IndexOf("Encrypted", StringComparison.OrdinalIgnoreCase) >= 0, "Encrypted flag missing.");
                Require(result.Flags.IndexOf("Aes", StringComparison.OrdinalIgnoreCase) >= 0, "AES flag missing.");
                Require(Encoding.UTF8.GetString(result.Payload) == "stateforge-envelope", "Payload mismatch.");

                byte[] legacy = Encoding.UTF8.GetBytes("legacy-payload");
                StateForgeStfg2EnvelopeResult legacyResult = StateForgeStfg2Envelope.Unwrap(legacy);

                Require(!legacyResult.IsStfg2, "Legacy payload was incorrectly detected as STFG2.");
                Require(Encoding.UTF8.GetString(legacyResult.Payload) == "legacy-payload", "Legacy payload mismatch.");

                Console.WriteLine("PASS: STFG2 envelope wrap");
                Console.WriteLine("PASS: STFG2 envelope unwrap");
                Console.WriteLine("PASS: STFG2 envelope KeyId");
                Console.WriteLine("PASS: STFG2 envelope flags");
                Console.WriteLine("PASS: STFG1 compatibility passthrough");

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
