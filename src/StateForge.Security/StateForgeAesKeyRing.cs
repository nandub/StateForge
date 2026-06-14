using System;
using System.Collections.Generic;

namespace StateForge.Security
{
    /// <summary>Contains the current AES key and retained keys needed to read older records.</summary>
    public sealed class StateForgeAesKeyRing
    {
        /// <summary>Gets or sets the key-ring serialization version.</summary>
        public string Version { get; set; }

        /// <summary>Gets or sets the identifier of the key used for new encryption operations.</summary>
        public string CurrentKeyId { get; set; }

        /// <summary>Gets the mutable collection of keys in the ring.</summary>
        public List<StateForgeAesKeyInfo> Keys { get; private set; }

        /// <summary>Initializes an empty version 1 key ring.</summary>
        public StateForgeAesKeyRing()
        {
            Version = "1";
            Keys = new List<StateForgeAesKeyInfo>();
        }

        /// <summary>Finds the key named by <see cref="CurrentKeyId"/>.</summary>
        /// <returns>The current key, or <see langword="null"/> when it is not configured or cannot be found.</returns>
        public StateForgeAesKeyInfo GetCurrentKey()
        {
            if (string.IsNullOrWhiteSpace(CurrentKeyId))
            {
                return null;
            }

            for (int i = 0; i < Keys.Count; i++)
            {
                if (string.Equals(Keys[i].KeyId, CurrentKeyId, StringComparison.OrdinalIgnoreCase))
                {
                    return Keys[i];
                }
            }

            return null;
        }

        /// <summary>Finds a key by identifier using an ordinal, case-insensitive comparison.</summary>
        /// <param name="keyId">The key identifier.</param>
        /// <returns>The matching key, or <see langword="null"/> when no match exists.</returns>
        public StateForgeAesKeyInfo FindKey(string keyId)
        {
            if (string.IsNullOrWhiteSpace(keyId))
            {
                return null;
            }

            for (int i = 0; i < Keys.Count; i++)
            {
                if (string.Equals(Keys[i].KeyId, keyId, StringComparison.OrdinalIgnoreCase))
                {
                    return Keys[i];
                }
            }

            return null;
        }
    }
}
