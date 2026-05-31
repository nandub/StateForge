using System;
using System.Collections.Specialized;
using System.IO;
using System.Web;
using System.Web.SessionState;
using StateForge.AspNet;

namespace StateForge.AspNetHarness
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            string root = ReadOption(args, "--root");
            bool keep = HasSwitch(args, "--keep");

            if (string.IsNullOrWhiteSpace(root))
            {
                root = Path.Combine(Path.GetTempPath(), "StateForgeAspNetHarness", Guid.NewGuid().ToString("N"));
            }

            root = Path.GetFullPath(root);

            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            Directory.CreateDirectory(root);

            try
            {
                Console.WriteLine("StateForge ASP.NET Provider Harness");
                Console.WriteLine("-----------------------------------");
                Console.WriteLine("RootPath: {0}", root);

                HttpContext context = CreateContext(root);
                StateForgeSessionStateProvider provider = CreateProvider(root, context);

                string id = "aspnet-harness-session";
                bool locked;
                TimeSpan lockAge;
                object lockId;
                SessionStateActions actions;

                provider.CreateUninitializedItem(context, id, 20);

                SessionStateStoreData first = provider.GetItem(context, id, out locked, out lockAge, out lockId, out actions);
                Require(first != null, "GetItem returned null after CreateUninitializedItem.");
                Require(!locked, "Item was unexpectedly locked after CreateUninitializedItem.");

                SessionStateStoreData exclusive = provider.GetItemExclusive(context, id, out locked, out lockAge, out lockId, out actions);
                Require(exclusive != null, "GetItemExclusive returned null.");
                Require(!locked, "GetItemExclusive returned locked=true.");

                exclusive.Items["Counter"] = 1;
                provider.SetAndReleaseItemExclusive(context, id, exclusive, lockId, true);

                SessionStateStoreData second = provider.GetItem(context, id, out locked, out lockAge, out lockId, out actions);
                Require(second != null, "GetItem returned null after SetAndReleaseItemExclusive.");
                Require(Convert.ToInt32(second.Items["Counter"]) == 1, "Counter session item mismatch.");

                provider.ResetItemTimeout(context, id);

                SessionStateStoreData exclusive2 = provider.GetItemExclusive(context, id, out locked, out lockAge, out lockId, out actions);
                Require(exclusive2 != null, "Second GetItemExclusive returned null.");
                exclusive2.Items["Counter"] = 2;
                provider.SetAndReleaseItemExclusive(context, id, exclusive2, lockId, false);

                SessionStateStoreData final = provider.GetItem(context, id, out locked, out lockAge, out lockId, out actions);
                Require(final != null, "Final GetItem returned null.");
                Require(Convert.ToInt32(final.Items["Counter"]) == 2, "Final Counter session item mismatch.");

                provider.RemoveItem(context, id, lockId, final);
                SessionStateStoreData removed = provider.GetItem(context, id, out locked, out lockAge, out lockId, out actions);
                Require(removed == null, "RemoveItem did not remove the session.");

                provider.EndRequest(context);
                provider.Dispose();

                Console.WriteLine();
                Console.WriteLine("PASS: CreateUninitializedItem");
                Console.WriteLine("PASS: GetItem");
                Console.WriteLine("PASS: GetItemExclusive");
                Console.WriteLine("PASS: SetAndReleaseItemExclusive");
                Console.WriteLine("PASS: ResetItemTimeout");
                Console.WriteLine("PASS: RemoveItem");

                if (!keep)
                {
                    Directory.Delete(root, true);
                }
                else
                {
                    Console.WriteLine("Kept harness store: {0}", root);
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: {0}: {1}", ex.GetType().Name, ex.Message);
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static HttpContext CreateContext(string root)
        {
            string requestUrl = "http://localhost/stateforge-harness.aspx";
            StringWriter writer = new StringWriter();
            HttpRequest request = new HttpRequest("stateforge-harness.aspx", requestUrl, string.Empty);
            HttpResponse response = new HttpResponse(writer);
            HttpContext context = new HttpContext(request, response);

            context.Items["StateForgeHarnessRoot"] = root;
            return context;
        }

        private static StateForgeSessionStateProvider CreateProvider(string root, HttpContext context)
        {
            StateForgeSessionStateProvider provider = new StateForgeSessionStateProvider();

            NameValueCollection config = new NameValueCollection();
            config["rootPath"] = root;
            config["defaultTimeoutMinutes"] = "20";
            config["staleLockMinutes"] = "1";
            config["shardDepth"] = "1";
            config["enableCompression"] = "true";
            config["enableEncryption"] = "false";
            config["keepBackups"] = "false";

            provider.Initialize("StateForgeHarness", config);
            provider.InitializeRequest(context);

            return provider;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static string ReadOption(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static bool HasSwitch(string[] args, string name)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
