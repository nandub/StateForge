using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.IO;
using System.Web;
using System.Web.SessionState;
using StateForge.Core;
using StateForge.FileStore;

namespace StateForge.AspNet
{
    public sealed class StateForgeSessionStateProvider : SessionStateStoreProviderBase
    {
        private IStateForgeStore _store;
        private int _staleLockMinutes;
        private int _defaultTimeoutMinutes;

        public override void Initialize(string name, NameValueCollection config)
        {
            if (string.IsNullOrEmpty(name)) { name = "StateForge"; }
            if (config == null) { throw new ArgumentNullException("config"); }

            base.Initialize(name, config);

            string rootPath = config["rootPath"];
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "StateForge");
            }

            _staleLockMinutes = ReadInt(config, "staleLockMinutes", 5);
            _defaultTimeoutMinutes = ReadInt(config, "defaultTimeoutMinutes", 20);

            StateForgeFileStoreOptions options = new StateForgeFileStoreOptions();
            options.RootPath = rootPath;
            options.StaleLockMinutes = _staleLockMinutes;
            options.ShardDepth = ReadInt(config, "shardDepth", 1);
            options.MaxPayloadBytes = ReadInt(config, "maxPayloadBytes", 104857600);
            options.MutexTimeoutMilliseconds = ReadInt(config, "mutexTimeoutMilliseconds", 30000);
            options.EnableCompression = ReadBool(config, "enableCompression", false);
            options.EnableEncryption = ReadBool(config, "enableEncryption", false);
            options.KeepBackups = ReadBool(config, "keepBackups", false);
            options.ProtectionMode = ReadProtectionMode(config["protectionMode"]);
            options.AesKeyBase64 = config["aesKeyBase64"];

            _store = new StateForgeFileStore(options);

            config.Remove("rootPath");
            config.Remove("staleLockMinutes");
            config.Remove("defaultTimeoutMinutes");
            config.Remove("shardDepth");
            config.Remove("maxPayloadBytes");
            config.Remove("mutexTimeoutMilliseconds");
            config.Remove("enableCompression");
            config.Remove("enableEncryption");
            config.Remove("keepBackups");
            config.Remove("protectionMode");
            config.Remove("aesKeyBase64");

            if (config.Count > 0)
            {
                throw new ProviderException("Unrecognized StateForge provider attribute: " + config.GetKey(0));
            }
        }

        public override void InitializeRequest(HttpContext context) { }

        public override SessionStateStoreData CreateNewStoreData(HttpContext context, int timeout)
        {
            return new SessionStateStoreData(new SessionStateItemCollection(), SessionStateUtility.GetSessionStaticObjects(context), timeout);
        }

        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            return GetItemInternal(context, id, false, out locked, out lockAge, out lockId, out actions);
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            return GetItemInternal(context, id, true, out locked, out lockAge, out lockId, out actions);
        }

        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData item, object lockId, bool newItem)
        {
            byte[] payload = SessionStatePayloadSerializer.Serialize((SessionStateItemCollection)item.Items);
            _store.SetAndUnlock(id, payload, TimeSpan.FromMinutes(item.Timeout), ConvertLockId(lockId));
        }

        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            _store.Unlock(id, ConvertLockId(lockId));
        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            _store.Remove(id);
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
            _store.Refresh(id, TimeSpan.FromMinutes(_defaultTimeoutMinutes));
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            _store.Set(id, SessionStatePayloadSerializer.Serialize(new SessionStateItemCollection()), TimeSpan.FromMinutes(timeout));
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback) { return false; }
        public override void EndRequest(HttpContext context) { }
        public override void Dispose() { }

        private SessionStateStoreData GetItemInternal(HttpContext context, string id, bool exclusive, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            locked = false;
            lockAge = TimeSpan.Zero;
            lockId = null;
            actions = SessionStateActions.None;

            if (exclusive)
            {
                StateForgeLockResult result = _store.GetAndLock(id, TimeSpan.FromMinutes(_staleLockMinutes));
                if (!result.Found) { return null; }

                if (result.LockedByOtherRequest)
                {
                    locked = true;
                    lockAge = result.LockAge;
                    lockId = result.LockId;
                    return null;
                }

                lockId = result.LockId;
                return CreateStoreData(context, result.Entry);
            }

            StateForgeEntry entry = _store.Get(id);
            return entry == null ? null : CreateStoreData(context, entry);
        }

        private static SessionStateStoreData CreateStoreData(HttpContext context, StateForgeEntry entry)
        {
            SessionStateItemCollection items = SessionStatePayloadSerializer.Deserialize(entry.Value);
            int timeoutMinutes = (int)Math.Ceiling(entry.ExpiresUtc.Subtract(DateTimeOffset.UtcNow).TotalMinutes);
            if (timeoutMinutes < 1) { timeoutMinutes = 1; }

            return new SessionStateStoreData(items, SessionStateUtility.GetSessionStaticObjects(context), timeoutMinutes);
        }

        private static long ConvertLockId(object lockId)
        {
            return lockId == null ? 0 : Convert.ToInt64(lockId);
        }

        private static int ReadInt(NameValueCollection config, string key, int defaultValue)
        {
            int parsed;
            return int.TryParse(config[key], out parsed) && parsed >= 1 ? parsed : defaultValue;
        }

        private static StateForgeProtectionMode ReadProtectionMode(string value)
        {
            if (string.Equals(value, "aes", StringComparison.OrdinalIgnoreCase))
            {
                return StateForgeProtectionMode.Aes;
            }

            if (string.Equals(value, "dpapi", StringComparison.OrdinalIgnoreCase))
            {
                return StateForgeProtectionMode.Dpapi;
            }

            return StateForgeProtectionMode.None;
        }

        private static bool ReadBool(NameValueCollection config, string key, bool defaultValue)
        {
            string value = config[key];
            bool parsed;
            return bool.TryParse(value, out parsed) ? parsed : defaultValue;
        }
    }
}
