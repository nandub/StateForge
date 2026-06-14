using System;
using System.Reflection;

namespace StateForge.CloudNative
{
    /// <summary>Applies supported <c>STATEFORGE_*</c> environment variables to an options object.</summary>
    public static class StateForgeEnvironmentOptions
    {
        /// <summary>Applies matching environment settings to compatible public option properties.</summary>
        /// <param name="options">A StateForge options object.</param>
        public static void Apply(object options)
        {
            if (options == null)
            {
                throw new ArgumentNullException("options");
            }

            ApplyString(options, "RootPath", Environment.GetEnvironmentVariable("STATEFORGE_ROOT_PATH"));

            bool boolValue;
            int intValue;

            if (TryReadBool(Environment.GetEnvironmentVariable("STATEFORGE_COMPRESSION"), out boolValue))
            {
                ApplyValue(options, "EnableCompression", boolValue);
            }

            if (TryReadBool(Environment.GetEnvironmentVariable("STATEFORGE_ENCRYPTION"), out boolValue))
            {
                ApplyValue(options, "EnableEncryption", boolValue);
            }

            if (TryReadBool(Environment.GetEnvironmentVariable("STATEFORGE_KEEP_BACKUPS"), out boolValue))
            {
                ApplyValue(options, "KeepBackups", boolValue);
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("STATEFORGE_SHARD_DEPTH"), out intValue))
            {
                ApplyValue(options, "ShardDepth", intValue);
            }

            if (int.TryParse(Environment.GetEnvironmentVariable("STATEFORGE_MUTEX_TIMEOUT_MS"), out intValue) && intValue > 0)
            {
                ApplyValue(options, "MutexTimeoutMilliseconds", intValue);
            }

            string aesKey = Environment.GetEnvironmentVariable("STATEFORGE_AES_KEY_BASE64");

            if (!string.IsNullOrWhiteSpace(aesKey))
            {
                ApplyString(options, "AesKeyBase64", aesKey);
                ApplyValue(options, "EnableEncryption", true);
                ApplyEnum(options, "ProtectionMode", "Aes");
            }

            string protectionMode = Environment.GetEnvironmentVariable("STATEFORGE_PROTECTION_MODE");

            if (!string.IsNullOrWhiteSpace(protectionMode))
            {
                if (string.Equals(protectionMode, "aes", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyValue(options, "EnableEncryption", true);
                    ApplyEnum(options, "ProtectionMode", "Aes");
                }
                else if (string.Equals(protectionMode, "dpapi", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyValue(options, "EnableEncryption", true);
                    ApplyEnum(options, "ProtectionMode", "Dpapi");
                }
                else if (string.Equals(protectionMode, "none", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyValue(options, "EnableEncryption", false);
                    ApplyEnum(options, "ProtectionMode", "None");
                }
            }
        }

        private static void ApplyString(object options, string propertyName, string value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                ApplyValue(options, propertyName, value);
            }
        }

        private static void ApplyValue(object options, string propertyName, object value)
        {
            PropertyInfo property = options.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            if (property == null || !property.CanWrite)
            {
                return;
            }

            if (value == null)
            {
                property.SetValue(options, null, null);
                return;
            }

            Type propertyType = property.PropertyType;
            Type valueType = value.GetType();

            if (propertyType.IsAssignableFrom(valueType))
            {
                property.SetValue(options, value, null);
                return;
            }

            object converted = Convert.ChangeType(value, propertyType);
            property.SetValue(options, converted, null);
        }

        private static void ApplyEnum(object options, string propertyName, string enumName)
        {
            PropertyInfo property = options.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);

            if (property == null || !property.CanWrite || !property.PropertyType.IsEnum)
            {
                return;
            }

            object value = Enum.Parse(property.PropertyType, enumName, true);
            property.SetValue(options, value, null);
        }

        private static bool TryReadBool(string value, out bool result)
        {
            result = false;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (bool.TryParse(value, out result))
            {
                return true;
            }

            if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "y", StringComparison.OrdinalIgnoreCase))
            {
                result = true;
                return true;
            }

            if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "n", StringComparison.OrdinalIgnoreCase))
            {
                result = false;
                return true;
            }

            return false;
        }
    }
}
