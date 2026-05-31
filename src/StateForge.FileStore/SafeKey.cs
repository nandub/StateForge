using System.Security.Cryptography;
using System.Text;

namespace StateForge.FileStore
{
    internal static class SafeKey
    {
        public static string Hash(string key)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(key ?? string.Empty);
                byte[] hash = sha256.ComputeHash(bytes);
                StringBuilder builder = new StringBuilder(hash.Length * 2);

                for (int i = 0; i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("X2"));
                }

                return builder.ToString();
            }
        }
    }
}
