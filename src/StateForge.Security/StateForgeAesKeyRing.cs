using System;
using System.Collections.Generic;

namespace StateForge.Security
{
    public sealed class StateForgeAesKeyRing
    {
        public string Version { get; set; }

        public string CurrentKeyId { get; set; }

        public List<StateForgeAesKeyInfo> Keys { get; private set; }

        public StateForgeAesKeyRing()
        {
            Version = "1";
            Keys = new List<StateForgeAesKeyInfo>();
        }

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
