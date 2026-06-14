using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StateForge.FileStore
{
    internal static class AesPayloadProtector
    {
        public const int AuthenticationTagLength = 32;
        public const int AuthenticationTrailerLength = AuthenticationTagLength + 1;
        public const byte AuthenticationTrailerMarker = 0xA5;

        public static byte[] Protect(byte[] value, string keyBase64)
        {
            if (value == null) value = new byte[0];

            byte[] key = DecodeKey(keyBase64);

            try
            {
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
            finally
            {
                Array.Clear(key, 0, key.Length);
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

            try
            {
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
            finally
            {
                Array.Clear(key, 0, key.Length);
            }
        }

        public static byte[] ComputeAuthenticationTag(byte[] value, int offset, int count, string keyBase64)
        {
            byte[] key = DecodeKey(keyBase64);
            byte[] context = Encoding.UTF8.GetBytes("StateForge-STFG1-HMAC-v1");
            byte[] authenticationKey;

            using (HMACSHA256 derivation = new HMACSHA256(key))
            {
                authenticationKey = derivation.ComputeHash(context);
            }

            try
            {
                using (HMACSHA256 hmac = new HMACSHA256(authenticationKey))
                {
                    return hmac.ComputeHash(value, offset, count);
                }
            }
            finally
            {
                Array.Clear(authenticationKey, 0, authenticationKey.Length);
                Array.Clear(key, 0, key.Length);
            }
        }

        public static bool VerifyAuthenticationTag(
            byte[] value,
            int offset,
            int count,
            byte[] expectedTag,
            string keyBase64)
        {
            byte[] actualTag = ComputeAuthenticationTag(value, offset, count, keyBase64);

            try
            {
                if (expectedTag == null || expectedTag.Length != actualTag.Length)
                {
                    return false;
                }

                int difference = 0;
                for (int i = 0; i < actualTag.Length; i++)
                {
                    difference |= actualTag[i] ^ expectedTag[i];
                }

                return difference == 0;
            }
            finally
            {
                Array.Clear(actualTag, 0, actualTag.Length);
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
