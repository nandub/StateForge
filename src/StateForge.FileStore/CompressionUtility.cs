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

        public static byte[] Decompress(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                return new byte[0];
            }

            using (MemoryStream input = new MemoryStream(value))
            using (GZipStream gzip = new GZipStream(input, CompressionMode.Decompress))
            using (MemoryStream output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}
