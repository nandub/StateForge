using System;
using System.IO;
using System.Security.Cryptography;

namespace StateForge.FileStore
{
    internal static class AesPayloadProtector
    {
        public static byte[] Protect(byte[] value, string keyBase64)
        {
            if (value == null || value.Length == 0) return new byte[0];

            byte[] key = DecodeKey(keyBase64);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.GenerateIV();

                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                using (MemoryStream output = new MemoryStream())
                {
                    output.Write(aes.IV, 0, aes.IV.Length);

                    using (CryptoStream crypto = new CryptoStream(output, encryptor, CryptoStreamMode.Write))
                    {
                        crypto.Write(value, 0, value.Length);
                        crypto.FlushFinalBlock();
                    }

                    return output.ToArray();
                }
            }
        }

        public static byte[] Unprotect(byte[] value, string keyBase64)
        {
            if (value == null || value.Length == 0) return new byte[0];

            byte[] key = DecodeKey(keyBase64);

            if (value.Length < 17)
            {
                throw new CryptographicException("AES payload is too short.");
            }

            byte[] iv = new byte[16];
            Buffer.BlockCopy(value, 0, iv, 0, iv.Length);

            byte[] cipherText = new byte[value.Length - iv.Length];
            Buffer.BlockCopy(value, iv.Length, cipherText, 0, cipherText.Length);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                using (MemoryStream input = new MemoryStream(cipherText))
                using (CryptoStream crypto = new CryptoStream(input, decryptor, CryptoStreamMode.Read))
                using (MemoryStream output = new MemoryStream())
                {
                    crypto.CopyTo(output);
                    return output.ToArray();
                }
            }
        }

        private static byte[] DecodeKey(string keyBase64)
        {
            if (string.IsNullOrWhiteSpace(keyBase64))
            {
                throw new InvalidOperationException("AES protection requires AesKeyBase64.");
            }

            byte[] key = Convert.FromBase64String(keyBase64);

            if (key.Length != 16 && key.Length != 24 && key.Length != 32)
            {
                throw new InvalidOperationException("AES key must be 128, 192, or 256 bits.");
            }

            return key;
        }
    }
}
