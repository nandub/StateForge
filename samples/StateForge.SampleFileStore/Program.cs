using System;
using System.IO;
using System.Text;
using StateForge.Core;
using StateForge.FileStore;

string rootPath = ReadOption(args, "--root");
string aesKey = Environment.GetEnvironmentVariable("STATEFORGE_AES_KEY_BASE64");

if (string.IsNullOrWhiteSpace(rootPath))
{
    rootPath = Path.Combine(AppContext.BaseDirectory, "App_Data", "StateForge");
}

StateForgeFileStoreOptions options = new StateForgeFileStoreOptions
{
    RootPath = Path.GetFullPath(rootPath),
    EnableCompression = true,
    KeepBackups = false,
    ShardDepth = 1
};

if (!string.IsNullOrWhiteSpace(aesKey))
{
    options.EnableEncryption = true;
    options.ProtectionMode = StateForgeProtectionMode.Aes;
    options.AesKeyBase64 = aesKey;
}

StateForgeFileStore store = new StateForgeFileStore(options);
StateForgeValidationResult validation = store.ValidateConfiguration();

if (!validation.Success)
{
    Console.Error.WriteLine("Invalid StateForge configuration:");
    foreach (string error in validation.Errors)
    {
        Console.Error.WriteLine("  " + error);
    }

    return 1;
}

string command = args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal)
    ? args[0].ToLowerInvariant()
    : "demo";

switch (command)
{
    case "set":
        RequireArguments(args, 3, "set <key> <value>");
        store.Set(args[1], Encoding.UTF8.GetBytes(args[2]), TimeSpan.FromMinutes(20));
        Console.WriteLine("Stored key '{0}'.", args[1]);
        break;

    case "get":
        RequireArguments(args, 2, "get <key>");
        StateForgeEntry entry = store.Get(args[1]);
        Console.WriteLine(entry == null ? "(not found)" : Encoding.UTF8.GetString(entry.Value));
        break;

    case "remove":
        RequireArguments(args, 2, "remove <key>");
        Console.WriteLine(store.Remove(args[1]) ? "Removed." : "Not found.");
        break;

    case "list":
        foreach (StateForgeEntryInfo item in store.Enumerate())
        {
            Console.WriteLine(
                "{0} expires={1:o} bytes={2} compressed={3} aes={4}",
                item.Key,
                item.ExpiresUtc,
                item.PayloadLength,
                item.Compressed,
                item.AesEncrypted);
        }
        break;

    case "stats":
        StateForgeStoreStats stats = store.GetStats();
        Console.WriteLine("sessions={0}", stats.TotalSessions);
        Console.WriteLine("payloadBytes={0}", stats.TotalPayloadBytes);
        Console.WriteLine("compressed={0}", stats.CompressedSessions);
        Console.WriteLine("aesEncrypted={0}", stats.AesEncryptedSessions);
        break;

    case "demo":
        const string demoKey = "sample:counter";
        StateForgeEntry current = store.Get(demoKey);
        int counter = current == null ? 0 : int.Parse(Encoding.UTF8.GetString(current.Value));
        counter++;
        store.Set(demoKey, Encoding.UTF8.GetBytes(counter.ToString()), TimeSpan.FromMinutes(20));
        Console.WriteLine("Counter: {0}", counter);
        Console.WriteLine("RootPath: {0}", options.RootPath);
        break;

    default:
        Console.Error.WriteLine("Commands: demo, set, get, remove, list, stats");
        return 2;
}

return 0;

static string ReadOption(string[] arguments, string name)
{
    for (int i = 0; i < arguments.Length - 1; i++)
    {
        if (string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return arguments[i + 1];
        }
    }

    return null;
}

static void RequireArguments(string[] arguments, int count, string usage)
{
    if (arguments.Length < count)
    {
        throw new ArgumentException("Usage: " + usage);
    }
}
