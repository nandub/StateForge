using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace StateForge.Security
{
    public static class StateForgeAesKeyRingManager
    {
        public static StateForgeAesKeyRing CreateNew(string keyId)
        {
            StateForgeAesKeyRing ring = new StateForgeAesKeyRing();

            StateForgeAesKeyInfo key = CreateKey(keyId);
            ring.Keys.Add(key);
            ring.CurrentKeyId = key.KeyId;

            return ring;
        }

        public static StateForgeAesKeyInfo CreateKey(string keyId)
        {
            if (string.IsNullOrWhiteSpace(keyId))
            {
                keyId = "key-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            }

            byte[] keyBytes = new byte[32];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }

            StateForgeAesKeyInfo key = new StateForgeAesKeyInfo();
            key.KeyId = keyId;
            key.KeyBase64 = Convert.ToBase64String(keyBytes);
            key.CreatedUtc = DateTimeOffset.UtcNow;
            key.NotBeforeUtc = key.CreatedUtc;
            key.RetiredUtc = null;

            return key;
        }

        public static StateForgeAesKeyInfo Rotate(StateForgeAesKeyRing ring, string newKeyId, bool retirePrevious)
        {
            if (ring == null)
            {
                throw new ArgumentNullException("ring");
            }

            StateForgeAesKeyInfo previous = ring.GetCurrentKey();

            if (previous != null && retirePrevious)
            {
                previous.RetiredUtc = DateTimeOffset.UtcNow;
            }

            StateForgeAesKeyInfo next = CreateKey(newKeyId);
            ring.Keys.Add(next);
            ring.CurrentKeyId = next.KeyId;

            return next;
        }


        public static StateForgeAesKeyRingRotationResult RotateAndSave(string path, string newKeyId, bool retirePrevious)
        {
            StateForgeAesKeyRing ring = StateForgeAesKeyRingReader.Load(path);
            StateForgeAesKeyInfo previous = ring.GetCurrentKey();
            StateForgeAesKeyInfo next = Rotate(ring, newKeyId, retirePrevious);
            Save(path, ring);

            StateForgeAesKeyRingRotationResult result = new StateForgeAesKeyRingRotationResult();
            result.PreviousKeyId = previous == null ? null : previous.KeyId;
            result.CurrentKeyId = next.KeyId;
            result.KeyCount = ring.Keys.Count;
            result.RotatedUtc = DateTimeOffset.UtcNow;
            return result;
        }

        public static List<string> Validate(StateForgeAesKeyRing ring)
        {
            List<string> errors = new List<string>();

            if (ring == null)
            {
                errors.Add("Key ring is null.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(ring.CurrentKeyId))
            {
                errors.Add("CurrentKeyId is missing.");
            }

            if (ring.Keys == null || ring.Keys.Count == 0)
            {
                errors.Add("Key ring contains no keys.");
                return errors;
            }

            Dictionary<string, bool> ids = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < ring.Keys.Count; i++)
            {
                StateForgeAesKeyInfo key = ring.Keys[i];

                if (string.IsNullOrWhiteSpace(key.KeyId))
                {
                    errors.Add("A key has no KeyId.");
                }
                else if (ids.ContainsKey(key.KeyId))
                {
                    errors.Add("Duplicate KeyId: " + key.KeyId);
                }
                else
                {
                    ids[key.KeyId] = true;
                }

                ValidateAesKey(key.KeyBase64, errors, key.KeyId);
            }

            if (!string.IsNullOrWhiteSpace(ring.CurrentKeyId) && !ids.ContainsKey(ring.CurrentKeyId))
            {
                errors.Add("CurrentKeyId does not reference a key in the ring: " + ring.CurrentKeyId);
            }

            return errors;
        }

        public static void Save(string path, StateForgeAesKeyRing ring)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required.", "path");
            }

            string directory = Path.GetDirectoryName(Path.GetFullPath(path));

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, StateForgeAesKeyRingJson.ToJson(ring));
        }

        private static void ValidateAesKey(string keyBase64, List<string> errors, string keyId)
        {
            if (string.IsNullOrWhiteSpace(keyBase64))
            {
                errors.Add("KeyBase64 is missing for key: " + keyId);
                return;
            }

            try
            {
                byte[] key = Convert.FromBase64String(keyBase64);

                if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                {
                    errors.Add("AES key length must be 16, 24, or 32 bytes for key: " + keyId);
                }
            }
            catch
            {
                errors.Add("KeyBase64 is not valid Base64 for key: " + keyId);
            }
        }
    }
}
