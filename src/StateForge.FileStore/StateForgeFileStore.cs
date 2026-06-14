using System;
using System.Collections.Generic;
using System.IO;
using StateForge.Core;

namespace StateForge.FileStore
{
    /// <summary>Provides a file-backed implementation of the StateForge state-store contracts.</summary>
    /// <example>
    /// Create a store, write a UTF-8 value, and read it back:
    /// <code language="csharp">
    /// var options = new StateForgeFileStoreOptions
    /// {
    ///     RootPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "StateForge"),
    ///     EnableCompression = true,
    ///     KeepBackups = false
    /// };
    ///
    /// var store = new StateForgeFileStore(options);
    /// store.Set("sample:greeting", Encoding.UTF8.GetBytes("hello"), TimeSpan.FromMinutes(20));
    ///
    /// StateForgeEntry entry = store.Get("sample:greeting");
    /// string value = entry == null ? null : Encoding.UTF8.GetString(entry.Value);
    /// </code>
    /// </example>
    public sealed class StateForgeFileStore : IStateForgeStore
    {
        private readonly StateForgeFileStoreOptions _options;
        private readonly string _rootPath;
        private readonly string _sessionsPath;
        private readonly string _tempPath;
        private readonly string _backupPath;
        private readonly string _quarantinePath;

        /// <summary>Initializes a file-backed store and creates its operational directories.</summary>
        /// <param name="options">The store paths, limits, and payload-protection settings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><see cref="StateForgeOptions.RootPath"/> is blank.</exception>
        public StateForgeFileStore(StateForgeFileStoreOptions options)
        {
            if (options == null) { throw new ArgumentNullException("options"); }
            if (string.IsNullOrWhiteSpace(options.RootPath)) { throw new ArgumentException("RootPath is required.", "options"); }

            _options = options;
            _rootPath = options.RootPath;
            _sessionsPath = Path.Combine(options.RootPath, "sessions");
            _tempPath = Path.Combine(options.RootPath, "temp");
            _backupPath = Path.Combine(options.RootPath, "backups");
            _quarantinePath = Path.Combine(options.RootPath, "quarantine");

            Directory.CreateDirectory(_sessionsPath);
            Directory.CreateDirectory(_tempPath);
            Directory.CreateDirectory(_backupPath);
            Directory.CreateDirectory(_quarantinePath);

            CleanupTemporaryFiles();
        }

        /// <summary>Gets an unexpired entry without acquiring its application lock.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <returns>The entry, or <see langword="null"/> when it does not exist or has expired.</returns>
        public StateForgeEntry Get(string key)
        {
            string hash = SafeKey.Hash(key);
            using (new StateForgeKeyMutex(hash, _options.MutexTimeoutMilliseconds))
            {
                StateForgeEntry entry = ReadEntryByHash(hash);
                if (entry == null) { return null; }

                if (entry.IsExpired(DateTimeOffset.UtcNow))
                {
                    RemoveByHash(hash);
                    return null;
                }

                return entry;
            }
        }

        /// <summary>Gets an entry and attempts to acquire its exclusive application lock.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <param name="lockTimeout">The age after which an existing lock is considered stale.</param>
        /// <returns>A result describing whether the entry was found, acquired, or held by another request.</returns>
        /// <example>
        /// Use the returned lock ID as a fencing token when updating:
        /// <code language="csharp">
        /// StateForgeLockResult locked = store.GetAndLock("cart:42", TimeSpan.FromSeconds(30));
        ///
        /// if (locked.Found &amp;&amp; !locked.LockedByOtherRequest)
        /// {
        ///     byte[] updated = UpdateCart(locked.Entry.Value);
        ///     bool saved = store.SetAndUnlock(
        ///         "cart:42",
        ///         updated,
        ///         TimeSpan.FromMinutes(20),
        ///         locked.LockId);
        /// }
        /// </code>
        /// </example>
        public StateForgeLockResult GetAndLock(string key, TimeSpan lockTimeout)
        {
            string hash = SafeKey.Hash(key);
            using (new StateForgeKeyMutex(hash, _options.MutexTimeoutMilliseconds))
            {
                StateForgeEntry entry = ReadEntryByHash(hash);
                if (entry == null) { return StateForgeLockResult.NotFound(); }

                if (entry.IsExpired(DateTimeOffset.UtcNow))
                {
                    RemoveByHash(hash);
                    return StateForgeLockResult.NotFound();
                }

                DateTimeOffset now = DateTimeOffset.UtcNow;

                if (entry.Locked && entry.LockDateUtc.HasValue)
                {
                    TimeSpan lockAge = now.Subtract(entry.LockDateUtc.Value);
                    if (lockAge < lockTimeout)
                    {
                        return StateForgeLockResult.Locked(lockAge, entry.LockId);
                    }
                }

                entry.Locked = true;
                entry.LockDateUtc = now;
                entry.LockId = entry.LockId + 1;
                entry.UpdatedUtc = now;

                WriteEntryAtomicByHash(entry, hash);
                return StateForgeLockResult.Acquired(entry);
            }
        }

        /// <summary>Creates or replaces an entry and clears any existing application lock.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <param name="value">The binary payload. A <see langword="null"/> value is stored as an empty array.</param>
        /// <param name="timeout">The lifetime of the entry measured from this write.</param>
        public void Set(string key, byte[] value, TimeSpan timeout)
        {
            string hash = SafeKey.Hash(key);
            using (new StateForgeKeyMutex(hash, _options.MutexTimeoutMilliseconds))
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                StateForgeEntry existing = ReadEntryByHash(hash);

                StateForgeEntry entry = new StateForgeEntry();
                entry.Key = key;
                entry.Value = value ?? new byte[0];
                entry.CreatedUtc = existing == null ? now : existing.CreatedUtc;
                entry.UpdatedUtc = now;
                entry.ExpiresUtc = now.Add(timeout);
                entry.Locked = false;
                entry.LockId = existing == null ? 0 : existing.LockId;
                entry.LockDateUtc = null;

                WriteEntryAtomicByHash(entry, hash);
            }
        }

        /// <summary>Replaces an entry and releases its application lock when the fencing token matches.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <param name="value">The binary payload. A <see langword="null"/> value is stored as an empty array.</param>
        /// <param name="timeout">The new lifetime of the entry.</param>
        /// <param name="lockId">The lock token returned by <see cref="GetAndLock"/>.</param>
        /// <returns><see langword="true"/> when the update was written; otherwise, <see langword="false"/>.</returns>
        public bool SetAndUnlock(string key, byte[] value, TimeSpan timeout, long lockId)
        {
            string hash = SafeKey.Hash(key);
            using (new StateForgeKeyMutex(hash, _options.MutexTimeoutMilliseconds))
            {
                DateTimeOffset now = DateTimeOffset.UtcNow;
                StateForgeEntry existing = ReadEntryByHash(hash);

                if (existing != null && (!existing.Locked || existing.LockId != lockId))
                {
                    return false;
                }

                StateForgeEntry entry = new StateForgeEntry();
                entry.Key = key;
                entry.Value = value ?? new byte[0];
                entry.CreatedUtc = existing == null ? now : existing.CreatedUtc;
                entry.UpdatedUtc = now;
                entry.ExpiresUtc = now.Add(timeout);
                entry.Locked = false;
                entry.LockId = existing == null ? lockId : existing.LockId;
                entry.LockDateUtc = null;

                WriteEntryAtomicByHash(entry, hash);
                return true;
            }
        }

        /// <summary>Releases an application lock when the supplied fencing token matches.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <param name="lockId">The lock token returned by <see cref="GetAndLock"/>.</param>
        /// <returns><see langword="true"/> when the lock was released; otherwise, <see langword="false"/>.</returns>
        public bool Unlock(string key, long lockId)
        {
            string hash = SafeKey.Hash(key);
            using (new StateForgeKeyMutex(hash, _options.MutexTimeoutMilliseconds))
            {
                StateForgeEntry entry = ReadEntryByHash(hash);
                if (entry == null) { return false; }

                if (entry.Locked && entry.LockId == lockId)
                {
                    entry.Locked = false;
                    entry.LockDateUtc = null;
                    entry.UpdatedUtc = DateTimeOffset.UtcNow;
                    WriteEntryAtomicByHash(entry, hash);
                    return true;
                }

                return false;
            }
        }

        /// <summary>Removes an entry using the store's normal synchronized removal path.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <returns><see langword="true"/> when an entry file was removed; otherwise, <see langword="false"/>.</returns>
        /// <remarks>This implementation is equivalent to calling <see cref="Remove"/>.</remarks>
        public bool ForceRemove(string key)
        {
            return Remove(key);
        }

        /// <summary>Removes an entry.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <returns><see langword="true"/> when an entry file was removed; otherwise, <see langword="false"/>.</returns>
        public bool Remove(string key)
        {
            string hash = SafeKey.Hash(key);
            using (new StateForgeKeyMutex(hash, _options.MutexTimeoutMilliseconds))
            {
                return RemoveByHash(hash);
            }
        }

        /// <summary>Extends the expiration time of an existing unexpired entry.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <param name="timeout">The new lifetime measured from the refresh operation.</param>
        /// <returns><see langword="true"/> when the entry was refreshed; otherwise, <see langword="false"/>.</returns>
        public bool Refresh(string key, TimeSpan timeout)
        {
            string hash = SafeKey.Hash(key);
            using (new StateForgeKeyMutex(hash, _options.MutexTimeoutMilliseconds))
            {
                StateForgeEntry entry = ReadEntryByHash(hash);
                if (entry == null) { return false; }

                DateTimeOffset now = DateTimeOffset.UtcNow;
                if (entry.IsExpired(now))
                {
                    RemoveByHash(hash);
                    return false;
                }

                entry.ExpiresUtc = now.Add(timeout);
                entry.UpdatedUtc = now;
                WriteEntryAtomicByHash(entry, hash);
                return true;
            }
        }

        /// <summary>Deletes expired records and either quarantines or deletes invalid records.</summary>
        /// <param name="quarantineInvalid"><see langword="true"/> to move invalid records to quarantine; <see langword="false"/> to delete them.</param>
        /// <returns>Counts for expired, invalid, and failed cleanup operations.</returns>
        public StateForgeCleanupResult CleanupExpired(bool quarantineInvalid)
        {
            StateForgeCleanupResult result = new StateForgeCleanupResult();

            foreach (string file in Directory.GetFiles(_sessionsPath, "*.stfg", SearchOption.AllDirectories))
            {
                bool invalid;
                StateForgeEntry entry = ReadEntryFromPath(file, out invalid);

                if (invalid)
                {
                    if (quarantineInvalid)
                    {
                        if (Quarantine(file)) { result.InvalidQuarantined++; } else { result.Failed++; }
                    }
                    else
                    {
                        if (TryDelete(file)) { result.InvalidDeleted++; } else { result.Failed++; }
                    }

                    continue;
                }

                if (entry != null && entry.IsExpired(DateTimeOffset.UtcNow))
                {
                    if (TryDelete(file)) { result.ExpiredDeleted++; } else { result.Failed++; }
                }
            }

            return result;
        }

        /// <summary>Enumerates readable entry metadata without returning payload bytes.</summary>
        /// <returns>A lazy sequence of metadata records. Invalid records are omitted.</returns>
        public IEnumerable<StateForgeEntryInfo> Enumerate()
        {
            foreach (string file in Directory.GetFiles(_sessionsPath, "*.stfg", SearchOption.AllDirectories))
            {
                bool invalid;
                int flags;
                StateForgeEntry entry = ReadEntryFromPath(file, out invalid, out flags);

                if (entry == null) { continue; }

                StateForgeEntryInfo info = new StateForgeEntryInfo();
                info.Key = entry.Key;
                info.CreatedUtc = entry.CreatedUtc;
                info.UpdatedUtc = entry.UpdatedUtc;
                info.ExpiresUtc = entry.ExpiresUtc;
                info.Locked = entry.Locked;
                info.LockId = entry.LockId;
                info.PhysicalPath = file;
                info.PayloadLength = entry.Value == null ? 0 : entry.Value.LongLength;
                info.Expired = entry.IsExpired(DateTimeOffset.UtcNow);
                info.Compressed = (flags & StateForgeConstants.FlagCompressed) == StateForgeConstants.FlagCompressed;
                info.Encrypted = (flags & StateForgeConstants.FlagEncrypted) == StateForgeConstants.FlagEncrypted || (flags & StateForgeConstants.FlagAesEncrypted) == StateForgeConstants.FlagAesEncrypted;
                info.AesEncrypted = (flags & StateForgeConstants.FlagAesEncrypted) == StateForgeConstants.FlagAesEncrypted;
                yield return info;
            }
        }


        /// <summary>Calculates aggregate entry counts and payload sizes.</summary>
        /// <returns>The current store statistics.</returns>
        public StateForgeStoreStats GetStats()
        {
            StateForgeStoreStats stats = new StateForgeStoreStats();

            foreach (StateForgeEntryInfo item in Enumerate())
            {
                stats.TotalSessions++;
                stats.TotalPayloadBytes += item.PayloadLength;

                if (item.Expired)
                {
                    stats.ExpiredSessions++;
                }

                if (item.Locked)
                {
                    stats.LockedSessions++;
                }

                if (item.Compressed)
                {
                    stats.CompressedSessions++;
                }

                if (item.Encrypted)
                {
                    stats.EncryptedSessions++;
                }

                if (item.AesEncrypted)
                {
                    stats.AesEncryptedSessions++;
                }
            }

            if (stats.TotalSessions > 0)
            {
                stats.AveragePayloadBytes = stats.TotalPayloadBytes / stats.TotalSessions;
            }

            return stats;
        }


        /// <summary>Validates store paths, sharding, mutex timing, write access, and AES key configuration.</summary>
        /// <returns>Configuration errors and non-fatal warnings.</returns>
        public StateForgeValidationResult ValidateConfiguration()
        {
            StateForgeValidationResult result = new StateForgeValidationResult();

            if (string.IsNullOrWhiteSpace(_options.RootPath))
            {
                result.AddError("RootPath is required.");
                return result;
            }

            if (!Directory.Exists(_rootPath))
            {
                result.AddError("RootPath does not exist: " + _rootPath);
            }

            if (_options.ShardDepth < 0 || _options.ShardDepth > 2)
            {
                result.AddWarning("ShardDepth outside recommended range 0-2. Runtime clamps to 0-2.");
            }

            if (_options.MutexTimeoutMilliseconds < 1000)
            {
                result.AddWarning("MutexTimeoutMilliseconds is low. Recommended minimum is 1000.");
            }

            if (_options.EnableEncryption && ResolveProtectionMode() == StateForgeProtectionMode.Aes)
            {
                try
                {
                    byte[] key = System.Convert.FromBase64String(_options.AesKeyBase64 ?? string.Empty);

                    if (key.Length != 16 && key.Length != 24 && key.Length != 32)
                    {
                        result.AddError("AesKeyBase64 must decode to 16, 24, or 32 bytes.");
                    }
                }
                catch
                {
                    result.AddError("AesKeyBase64 is not valid Base64.");
                }
            }

            if (!CanWriteDirectory(_rootPath))
            {
                result.AddError("RootPath is not writable: " + _rootPath);
            }

            return result;
        }

        /// <summary>Runs write, read, lock, enumeration, and cleanup probes against a temporary entry.</summary>
        /// <returns>The capability results and any exception captured by the probe.</returns>
        public StateForgeHealthResult CheckHealth()
        {
            StateForgeHealthResult result = new StateForgeHealthResult();
            string key = "__stateforge_health_" + Guid.NewGuid().ToString("N");

            try
            {
                Set(key, new byte[] { 1, 2, 3 }, TimeSpan.FromMinutes(1));
                result.CanWrite = true;

                StateForgeEntry entry = Get(key);
                result.CanRead = entry != null && entry.Value != null && entry.Value.Length == 3;

                StateForgeLockResult lockResult = GetAndLock(key, TimeSpan.FromSeconds(10));
                result.CanLock = lockResult.Found && !lockResult.LockedByOtherRequest;

                if (lockResult.Found && !lockResult.LockedByOtherRequest)
                {
                    SetAndUnlock(key, new byte[] { 4, 5, 6 }, TimeSpan.FromMinutes(1), lockResult.LockId);
                }

                int count = 0;
                foreach (StateForgeEntryInfo item in Enumerate())
                {
                    count++;
                    if (count > 0)
                    {
                        break;
                    }
                }

                result.CanEnumerate = true;

                CleanupExpired(true);
                result.CanCleanup = true;

                Remove(key);
            }
            catch (Exception ex)
            {
                result.AddError(ex.GetType().Name + ": " + ex.Message);
                try { Remove(key); } catch { }
            }

            return result;
        }

        /// <summary>Gets operational directories and their current file counts.</summary>
        /// <returns>The current store diagnostics.</returns>
        public StateForgeStoreDiagnostics GetDiagnostics()
        {
            StateForgeStoreDiagnostics diagnostics = new StateForgeStoreDiagnostics();
            diagnostics.RootPath = _rootPath;
            diagnostics.SessionsPath = _sessionsPath;
            diagnostics.TempPath = _tempPath;
            diagnostics.BackupPath = _backupPath;
            diagnostics.QuarantinePath = _quarantinePath;
            diagnostics.SessionFileCount = CountFiles(_sessionsPath, "*.stfg", SearchOption.AllDirectories);
            diagnostics.TempFileCount = CountFiles(_tempPath, "*.tmp", SearchOption.TopDirectoryOnly);
            diagnostics.BackupFileCount = CountFiles(_backupPath, "*.bak", SearchOption.TopDirectoryOnly);
            diagnostics.QuarantineFileCount = CountFiles(_quarantinePath, "*.*", SearchOption.TopDirectoryOnly);
            return diagnostics;
        }

        private StateForgeEntry ReadEntryByHash(string hash)
        {
            bool invalid;
            string[] paths = GetCandidatePathsForHash(hash);

            for (int i = 0; i < paths.Length; i++)
            {
                StateForgeEntry entry = ReadEntryFromPath(paths[i], out invalid);

                if (entry != null)
                {
                    return entry;
                }
            }

            return null;
        }

        private StateForgeEntry ReadEntryFromPath(string path, out bool invalid)
        {
            int flags;
            return ReadEntryFromPath(path, out invalid, out flags);
        }

        private StateForgeEntry ReadEntryFromPath(string path, out bool invalid, out int flags)
        {
            invalid = false;
            flags = 0;

            if (!File.Exists(path)) { return null; }

            try
            {
                long maximumRecordBytes = (long)_options.MaxPayloadBytes + 1048576L;
                FileInfo fileInfo = new FileInfo(path);

                if (fileInfo.Length < 12 || fileInfo.Length > maximumRecordBytes)
                {
                    invalid = true;
                    return null;
                }

                byte[] fileBytes = File.ReadAllBytes(path);
                int recordLength = fileBytes.Length;

                if (fileBytes.Length < 12 || fileBytes.LongLength > maximumRecordBytes)
                {
                    invalid = true;
                    return null;
                }

                flags = BitConverter.ToInt32(fileBytes, 8);
                int knownFlags = StateForgeConstants.FlagCompressed |
                    StateForgeConstants.FlagEncrypted |
                    StateForgeConstants.FlagAesEncrypted |
                    StateForgeConstants.FlagAuthenticated;

                if ((flags & ~knownFlags) != 0 ||
                    ((flags & StateForgeConstants.FlagAuthenticated) != 0 &&
                     (flags & StateForgeConstants.FlagAesEncrypted) == 0))
                {
                    invalid = true;
                    return null;
                }

                if ((flags & StateForgeConstants.FlagAuthenticated) != 0)
                {
                    if (fileBytes.Length <= AesPayloadProtector.AuthenticationTrailerLength ||
                        fileBytes[fileBytes.Length - 1] != AesPayloadProtector.AuthenticationTrailerMarker)
                    {
                        invalid = true;
                        return null;
                    }

                    recordLength = fileBytes.Length - AesPayloadProtector.AuthenticationTrailerLength;
                    byte[] expectedTag = new byte[AesPayloadProtector.AuthenticationTagLength];
                    Buffer.BlockCopy(
                        fileBytes,
                        recordLength,
                        expectedTag,
                        0,
                        expectedTag.Length);

                    if (!AesPayloadProtector.VerifyAuthenticationTag(
                        fileBytes,
                        0,
                        recordLength,
                        expectedTag,
                        _options.AesKeyBase64))
                    {
                        invalid = true;
                        return null;
                    }
                }

                using (MemoryStream stream = new MemoryStream(fileBytes, 0, recordLength, false, true))
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int magic = reader.ReadInt32();
                    int version = reader.ReadInt32();

                    if (magic != StateForgeConstants.FileMagic || version != StateForgeConstants.FileVersion)
                    {
                        invalid = true;
                        return null;
                    }

                    flags = reader.ReadInt32();

                    StateForgeEntry entry = new StateForgeEntry();
                    entry.Key = reader.ReadString();
                    entry.CreatedUtc = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
                    entry.UpdatedUtc = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
                    entry.ExpiresUtc = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
                    entry.Locked = reader.ReadBoolean();
                    entry.LockId = reader.ReadInt64();

                    bool hasLockDate = reader.ReadBoolean();
                    if (hasLockDate)
                    {
                        entry.LockDateUtc = DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadInt64());
                    }

                    int length = reader.ReadInt32();
                    if (length < 0 || length > maximumRecordBytes)
                    {
                        invalid = true;
                        return null;
                    }

                    byte[] payload = reader.ReadBytes(length);
                    if (payload.Length != length)
                    {
                        invalid = true;
                        return null;
                    }

                    if (stream.Position != stream.Length)
                    {
                        invalid = true;
                        return null;
                    }

                    // Reverse the write pipeline: decrypt first, then decompress.
                    if ((flags & StateForgeConstants.FlagAesEncrypted) == StateForgeConstants.FlagAesEncrypted)
                    {
                        payload = AesPayloadProtector.Unprotect(payload, _options.AesKeyBase64);
                    }
                    else if ((flags & StateForgeConstants.FlagEncrypted) == StateForgeConstants.FlagEncrypted)
                    {
                        payload = DpapiPayloadProtector.Unprotect(payload);
                    }

                    if ((flags & StateForgeConstants.FlagCompressed) == StateForgeConstants.FlagCompressed)
                    {
                        payload = CompressionUtility.Decompress(payload, _options.MaxPayloadBytes);
                    }

                    if (payload.Length > _options.MaxPayloadBytes)
                    {
                        invalid = true;
                        return null;
                    }

                    entry.Value = payload;
                    return entry;
                }
            }
            catch
            {
                invalid = true;
                return null;
            }
        }

        private void WriteEntryAtomicByHash(StateForgeEntry entry, string hash)
        {
            string path = GetPathForHash(hash);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            string tempPath = Path.Combine(_tempPath, Guid.NewGuid().ToString("N") + ".tmp");
            string backupPath = Path.Combine(_backupPath, hash + ".bak");

            byte[] value = entry.Value ?? new byte[0];
            if (value.Length > _options.MaxPayloadBytes)
            {
                throw new InvalidOperationException("StateForge payload exceeds MaxPayloadBytes.");
            }

            int flags = 0;
            byte[] storedValue = value;

            // Write pipeline: compress first, then encrypt.
            if (_options.EnableCompression && storedValue.Length > 0)
            {
                storedValue = CompressionUtility.Compress(storedValue);
                flags = flags | StateForgeConstants.FlagCompressed;
            }

            StateForgeProtectionMode mode = ResolveProtectionMode();

            if (mode == StateForgeProtectionMode.Aes)
            {
                storedValue = AesPayloadProtector.Protect(storedValue, _options.AesKeyBase64);
                flags = flags |
                    StateForgeConstants.FlagAesEncrypted |
                    StateForgeConstants.FlagAuthenticated;
            }
            else if (mode == StateForgeProtectionMode.Dpapi && storedValue.Length > 0)
            {
                storedValue = DpapiPayloadProtector.Protect(storedValue);
                flags = flags | StateForgeConstants.FlagEncrypted;
            }

            using (FileStream stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(StateForgeConstants.FileMagic);
                writer.Write(StateForgeConstants.FileVersion);
                writer.Write(flags);
                writer.Write(entry.Key ?? string.Empty);
                writer.Write(entry.CreatedUtc.ToUnixTimeMilliseconds());
                writer.Write(entry.UpdatedUtc.ToUnixTimeMilliseconds());
                writer.Write(entry.ExpiresUtc.ToUnixTimeMilliseconds());
                writer.Write(entry.Locked);
                writer.Write(entry.LockId);
                writer.Write(entry.LockDateUtc.HasValue);

                if (entry.LockDateUtc.HasValue)
                {
                    writer.Write(entry.LockDateUtc.Value.ToUnixTimeMilliseconds());
                }

                writer.Write(storedValue.Length);
                writer.Write(storedValue);
            }

            if ((flags & StateForgeConstants.FlagAuthenticated) != 0)
            {
                byte[] recordBytes = File.ReadAllBytes(tempPath);
                byte[] tag = AesPayloadProtector.ComputeAuthenticationTag(
                    recordBytes,
                    0,
                    recordBytes.Length,
                    _options.AesKeyBase64);

                using (FileStream stream = new FileStream(tempPath, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    stream.Write(tag, 0, tag.Length);
                    stream.WriteByte(AesPayloadProtector.AuthenticationTrailerMarker);
                }
            }

            try
            {
                if (File.Exists(path))
                {
                    if (_options.KeepBackups)
                    {
                        File.Replace(tempPath, path, backupPath, true);
                    }
                    else
                    {
                        File.Delete(path);
                        File.Move(tempPath, path);
                    }
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            finally
            {
                TryDelete(tempPath);
            }
        }



        private static bool CanWriteDirectory(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    return false;
                }

                string testFile = Path.Combine(path, ".stateforge-write-test-" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private StateForgeProtectionMode ResolveProtectionMode()
        {
            if (!_options.EnableEncryption)
            {
                return StateForgeProtectionMode.None;
            }

            if (_options.ProtectionMode == StateForgeProtectionMode.Aes)
            {
                return StateForgeProtectionMode.Aes;
            }

            if (_options.ProtectionMode == StateForgeProtectionMode.Dpapi)
            {
                return StateForgeProtectionMode.Dpapi;
            }

            if (_options.UseWindowsDpapi)
            {
                return StateForgeProtectionMode.Dpapi;
            }

            return StateForgeProtectionMode.None;
        }

        private bool RemoveByHash(string hash)
        {
            bool removed = false;
            string[] paths = GetCandidatePathsForHash(hash);

            for (int i = 0; i < paths.Length; i++)
            {
                if (TryDelete(paths[i]))
                {
                    removed = true;
                }
            }

            return removed;
        }


        private string[] GetCandidatePathsForHash(string hash)
        {
            List<string> paths = new List<string>();
            AddUniquePath(paths, GetPathForHash(hash));

            // Rolling upgrade compatibility. Reads and deletes can see the
            // configured path plus legacy/uncommon shard depths.
            AddUniquePath(paths, GetPathForHash(hash, 0));
            AddUniquePath(paths, GetPathForHash(hash, 1));
            AddUniquePath(paths, GetPathForHash(hash, 2));

            return paths.ToArray();
        }

        private static void AddUniquePath(List<string> paths, string path)
        {
            for (int i = 0; i < paths.Count; i++)
            {
                if (string.Equals(paths[i], path, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            paths.Add(path);
        }

        private string GetPathForHash(string hash, int depth)
        {
            string path = _sessionsPath;

            if (depth < 0) { depth = 0; }
            if (depth > 2) { depth = 2; }

            for (int i = 0; i < depth; i++)
            {
                path = Path.Combine(path, hash.Substring(i * 2, 2));
            }

            return Path.Combine(path, hash + ".stfg");
        }

        private string GetPathForHash(string hash)
        {
            return GetPathForHash(hash, _options.ShardDepth);
        }

        private bool Quarantine(string path)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(path) + "." + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".bad";
                File.Move(path, Path.Combine(_quarantinePath, name));
                return true;
            }
            catch { return false; }
        }

        private void CleanupTemporaryFiles()
        {
            foreach (string file in Directory.GetFiles(_tempPath, "*.tmp", SearchOption.TopDirectoryOnly))
            {
                TryDelete(file);
            }
        }

        private static int CountFiles(string path, string pattern, SearchOption searchOption)
        {
            try
            {
                if (!Directory.Exists(path)) { return 0; }
                return Directory.GetFiles(path, pattern, searchOption).Length;
            }
            catch { return 0; }
        }

        private static bool TryDelete(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
            }
            catch { }
            return false;
        }
    }
}
