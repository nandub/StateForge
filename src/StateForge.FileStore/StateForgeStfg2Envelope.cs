using System;
using StateForge.Format;

namespace StateForge.FileStore
{
    public static class StateForgeStfg2Envelope
    {
        public static byte[] Wrap(byte[] payload, bool compressed, bool encrypted, bool aes, bool dpapi, string keyId)
        {
            if (payload == null)
            {
                throw new ArgumentNullException("payload");
            }

            StateForgeFormatFlags flags = StateForgeFormatFlags.None;

            if (compressed)
            {
                flags |= StateForgeFormatFlags.Compressed;
            }

            if (encrypted)
            {
                flags |= StateForgeFormatFlags.Encrypted;
            }

            if (aes)
            {
                flags |= StateForgeFormatFlags.Aes;
            }

            if (dpapi)
            {
                flags |= StateForgeFormatFlags.Dpapi;
            }

            return StateForgeStfg2.Write(payload, flags, keyId);
        }

        public static StateForgeStfg2EnvelopeResult Unwrap(byte[] fileBytes)
        {
            if (fileBytes == null)
            {
                throw new ArgumentNullException("fileBytes");
            }

            StateForgeStfg2EnvelopeResult result = new StateForgeStfg2EnvelopeResult();

            if (!StateForgeStfg2.IsStfg2(fileBytes))
            {
                result.IsStfg2 = false;
                result.ChecksumValid = false;
                result.KeyId = string.Empty;
                result.Payload = fileBytes;
                result.Flags = string.Empty;
                return result;
            }

            StateForgeStfg2ReadResult read = StateForgeStfg2.Read(fileBytes);
            result.IsStfg2 = true;
            result.ChecksumValid = read.ChecksumValid;
            result.KeyId = read.KeyId;
            result.Payload = read.Payload;
            result.Flags = read.Flags.ToString();

            return result;
        }
    }
}
