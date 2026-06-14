using System.IO;
using System.IO.Compression;

namespace StateForge.FileStore
{
    internal static class CompressionUtility
    {
        public static byte[] Compress(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                return new byte[0];
            }

            using (MemoryStream output = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(output, CompressionMode.Compress))
                {
                    gzip.Write(value, 0, value.Length);
                }

                return output.ToArray();
            }
        }

        public static byte[] Decompress(byte[] value, int maxOutputBytes)
        {
            if (value == null || value.Length == 0)
            {
                return new byte[0];
            }

            using (MemoryStream input = new MemoryStream(value))
            using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
            using (MemoryStream output = new MemoryStream())
            {
                byte[] buffer = new byte[81920];
                int read;

                while ((read = gzip.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (output.Length + read > maxOutputBytes)
                    {
                        throw new InvalidDataException("Decompressed payload exceeds MaxPayloadBytes.");
                    }

                    output.Write(buffer, 0, read);
                }

                return output.ToArray();
            }
        }
    }
}
