using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace StateForge.Security
{
    /// <summary>Creates, validates, rotates, and atomically persists StateForge AES key rings.</summary>
    /// <remarks>
    /// Generated keys contain 256 random bits. Accepted AES key sizes follow
    /// <see href="https://csrc.nist.gov/pubs/fips/197/final">NIST FIPS 197</see>.
    /// </remarks>
    public static class StateForgeAesKeyRingManager
    {
        /// <summary>Creates a key ring containing one current key.</summary>
        /// <param name="keyId">The key identifier, or a blank value to generate a timestamp-based identifier.</param>
        /// <returns>A new key ring.</returns>
        /// <example>
        /// Create, save, and later rotate a key ring:
        /// <code language="csharp">
        /// string path = Path.Combine(AppContext.BaseDirectory, "stateforge-keyring.json");
        ///
        /// StateForgeAesKeyRing ring = StateForgeAesKeyRingManager.CreateNew("key-001");
        /// StateForgeAesKeyRingManager.Save(path, ring);
        ///
        /// StateForgeAesKeyRingRotationResult rotation =
        ///     StateForgeAesKeyRingManager.RotateAndSave(path, "key-002", true);
        /// </code>
        /// </example>
        public static StateForgeAesKeyRing CreateNew(string keyId)
        {
            StateForgeAesKeyRing ring = new StateForgeAesKeyRing();

            StateForgeAesKeyInfo key = CreateKey(keyId);
            ring.Keys.Add(key);
            ring.CurrentKeyId = key.KeyId;

            return ring;
        }

        /// <summary>Creates a 256-bit AES key using a cryptographic random-number generator.</summary>
        /// <param name="keyId">The key identifier, or a blank value to generate a timestamp-based identifier.</param>
        /// <returns>The new key metadata and Base64-encoded key material.</returns>
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

        /// <summary>Adds a new current key to an existing ring.</summary>
        /// <param name="ring">The key ring to rotate.</param>
        /// <param name="newKeyId">The new key identifier, or a blank value to generate one.</param>
        /// <param name="retirePrevious"><see langword="true"/> to timestamp the previous current key as retired.</param>
        /// <returns>The newly created current key.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="ring"/> is <see langword="null"/>.</exception>
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


        /// <summary>Loads, rotates, validates, and atomically saves a key ring.</summary>
        /// <param name="path">The key-ring JSON path.</param>
        /// <param name="newKeyId">The new key identifier, or a blank value to generate one.</param>
        /// <param name="retirePrevious"><see langword="true"/> to timestamp the previous current key as retired.</param>
        /// <returns>A summary of the rotation.</returns>
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

        /// <summary>Validates key identifiers, current-key membership, Base64 encoding, and AES key sizes.</summary>
        /// <param name="ring">The key ring to validate.</param>
        /// <returns>A list of validation errors; an empty list indicates success.</returns>
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

        /// <summary>Validates and atomically writes a key ring to disk.</summary>
        /// <param name="path">The destination JSON path.</param>
        /// <param name="ring">The key ring to persist.</param>
        /// <exception cref="ArgumentException"><paramref name="path"/> is blank.</exception>
        /// <exception cref="InvalidOperationException">Key-ring validation fails.</exception>
        public static void Save(string path, StateForgeAesKeyRing ring)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path is required.", "path");
            }

            List<string> errors = Validate(ring);
            if (errors.Count > 0)
            {
                throw new InvalidOperationException("Key ring validation failed: " + string.Join("; ", errors.ToArray()));
            }

            string fullPath = Path.GetFullPath(path);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string tempPath = Path.Combine(directory, "." + Path.GetFileName(fullPath) + "." + Guid.NewGuid().ToString("N") + ".tmp");

            try
            {
                using (FileStream stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(StateForgeAesKeyRingJson.ToJson(ring));
                    writer.Flush();
                    stream.Flush();
                }

                if (File.Exists(fullPath))
                {
                    File.Replace(tempPath, fullPath, null, true);
                }
                else
                {
                    File.Move(tempPath, fullPath);
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
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
