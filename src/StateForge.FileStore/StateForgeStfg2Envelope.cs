using System;
using StateForge.Format;

namespace StateForge.FileStore
{
    /// <summary>Adapts opaque StateForge record bytes to and from the STFG2 envelope format.</summary>
    /// <remarks>
    /// This adapter records transformation flags but does not itself compress, encrypt, decrypt, or
    /// reinterpret the payload.
    /// </remarks>
    public static class StateForgeStfg2Envelope
    {
        /// <summary>Wraps opaque payload bytes in an STFG2 envelope.</summary>
        /// <param name="payload">The already transformed payload bytes.</param>
        /// <param name="compressed">Indicates that the payload is compressed.</param>
        /// <param name="encrypted">Indicates that the payload is encrypted.</param>
        /// <param name="aes">Indicates that AES encryption was used.</param>
        /// <param name="dpapi">Indicates that Windows DPAPI protection was used.</param>
        /// <param name="keyId">The optional encryption key identifier.</param>
        /// <returns>The complete STFG2 envelope.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
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

        /// <summary>Reads an STFG2 envelope or returns legacy bytes unchanged.</summary>
        /// <param name="fileBytes">The complete record bytes.</param>
        /// <returns>The envelope metadata and opaque payload.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileBytes"/> is <see langword="null"/>.</exception>
        /// <remarks>
        /// For non-STFG2 input, <see cref="StateForgeStfg2EnvelopeResult.IsStfg2"/> and
        /// <see cref="StateForgeStfg2EnvelopeResult.ChecksumValid"/> are <see langword="false"/>,
        /// and <see cref="StateForgeStfg2EnvelopeResult.Payload"/> contains the original bytes.
        /// </remarks>
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
