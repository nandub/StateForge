using System.Security.Cryptography;

namespace StateForge.FileStore
{
    internal static class DpapiPayloadProtector
    {
        public static byte[] Protect(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                return new byte[0];
            }

            return ProtectedData.Protect(value, null, DataProtectionScope.LocalMachine);
        }

        public static byte[] Unprotect(byte[] value)
        {
            if (value == null || value.Length == 0)
            {
                return new byte[0];
            }

            return ProtectedData.Unprotect(value, null, DataProtectionScope.LocalMachine);
        }
    }
}
