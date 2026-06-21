using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Grpc.Net.Client;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StateForge.Core;
using StateForge.Remote.Protocol;

namespace StateForge.Remote.Tests
{
    [TestClass]
    public sealed class StateForgeRemoteIntegrationTests
    {
        [TestMethod]
        [TestCategory("Integration")]
        public void RemoteStoreRoundTripsThroughTlsGrpcHost()
        {
            string temporaryRoot = Path.Combine(Path.GetTempPath(), "StateForgeRemoteTests", Guid.NewGuid().ToString("N"));
            string storeRoot = Path.Combine(temporaryRoot, "store");
            string certificatePath = Path.Combine(temporaryRoot, "stateforge-remote-test.pfx");
            string certificatePassword = Guid.NewGuid().ToString("N");
            string bearerToken = "remote-integration-" + Guid.NewGuid().ToString("N");
            string aesKey = CreateAesKey();
            string certificateThumbprint = CreateCertificate(certificatePath, certificatePassword);
            int port = GetFreeTcpPort();
            StringBuilder output = new StringBuilder();
            Process hostProcess = null;

            Directory.CreateDirectory(storeRoot);

            try
            {
                hostProcess = StartRemoteHost(
                    port,
                    storeRoot,
                    certificatePath,
                    certificatePassword,
                    aesKey,
                    bearerToken,
                    output);

                WaitForHost(port, certificateThumbprint, hostProcess, output);
                WaitForGrpcReady(port, certificateThumbprint, bearerToken, hostProcess, output);

                using (GrpcChannel channel = CreateChannel(port, certificateThumbprint, bearerToken))
                {
                    var client = new StateForgeStoreRpc.StateForgeStoreRpcClient(channel);
                    RemoteStateForgeStore store = CreateStore(client, port, bearerToken);

                    string key = "remote:e2e:" + Guid.NewGuid().ToString("N");
                    byte[] initialValue = Encoding.UTF8.GetBytes("hello remote");
                    byte[] updatedValue = Encoding.UTF8.GetBytes("updated remote");

                    store.Set(key, initialValue, TimeSpan.FromMinutes(20));

                    StateForgeEntry entry = store.Get(key);
                    Assert.IsNotNull(entry, GetHostOutput(output));
                    CollectionAssert.AreEqual(initialValue, entry.Value);

                    StateForgeLockResult lockResult = store.GetAndLock(key, TimeSpan.FromSeconds(10));
                    Assert.IsTrue(lockResult.Found, GetHostOutput(output));
                    Assert.IsFalse(lockResult.LockedByOtherRequest, GetHostOutput(output));
                    Assert.IsTrue(lockResult.LockId > 0, GetHostOutput(output));

                    Assert.IsTrue(store.SetAndUnlock(key, updatedValue, TimeSpan.FromMinutes(20), lockResult.LockId), GetHostOutput(output));
                    Assert.IsTrue(store.Refresh(key, TimeSpan.FromMinutes(20)), GetHostOutput(output));

                    StateForgeEntry updatedEntry = store.Get(key);
                    Assert.IsNotNull(updatedEntry, GetHostOutput(output));
                    CollectionAssert.AreEqual(updatedValue, updatedEntry.Value);

                    StateForgeStoreStats stats = store.GetStats();
                    Assert.IsTrue(stats.TotalSessions >= 1, GetHostOutput(output));
                    Assert.IsTrue(stats.AesEncryptedSessions >= 1, GetHostOutput(output));

                    Assert.IsTrue(store.Remove(key), GetHostOutput(output));
                    Assert.IsNull(store.Get(key), GetHostOutput(output));
                }
            }
            finally
            {
                StopProcess(hostProcess);
                TryDeleteDirectory(temporaryRoot);
            }
        }

        private static Process StartRemoteHost(
            int port,
            string storeRoot,
            string certificatePath,
            string certificatePassword,
            string aesKey,
            string bearerToken,
            StringBuilder output)
        {
            string hostPath = GetHostAssemblyPath();
            var startInfo = new ProcessStartInfo("dotnet", "\"" + hostPath + "\"")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.EnvironmentVariables["STATEFORGE_REMOTE_LISTEN"] = "tcp:127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture);
            startInfo.EnvironmentVariables["STATEFORGE_REMOTE_TLS_CERT_PATH"] = certificatePath;
            startInfo.EnvironmentVariables["STATEFORGE_REMOTE_TLS_CERT_PASSWORD"] = certificatePassword;
            startInfo.EnvironmentVariables["STATEFORGE_ROOT_PATH"] = storeRoot;
            startInfo.EnvironmentVariables["STATEFORGE_AES_KEY_BASE64"] = aesKey;
            startInfo.EnvironmentVariables["STATEFORGE_REMOTE_BEARER_TOKEN"] = bearerToken;

            Process process = Process.Start(startInfo);
            if (process == null)
            {
                Assert.Fail("Failed to start StateForge.Remote.Host.");
            }

            process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs args)
            {
                if (args.Data != null)
                {
                    output.AppendLine(args.Data);
                }
            };
            process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs args)
            {
                if (args.Data != null)
                {
                    output.AppendLine(args.Data);
                }
            };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return process;
        }

        private static void WaitForHost(int port, string certificateThumbprint, Process process, StringBuilder output)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(30);
            string expectedAddress = "https://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture);

            while (DateTime.UtcNow < deadline)
            {
                if (process.HasExited)
                {
                    Assert.Fail("StateForge.Remote.Host exited before it became ready." + Environment.NewLine + GetHostOutput(output));
                }

                if (output.ToString().IndexOf(expectedAddress, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return;
                }

                Thread.Sleep(250);
            }

            Assert.Fail("Timed out waiting for StateForge.Remote.Host readiness." + Environment.NewLine + GetHostOutput(output));
        }

        private static void WaitForGrpcReady(
            int port,
            string certificateThumbprint,
            string bearerToken,
            Process process,
            StringBuilder output)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(30);
            Exception lastException = null;

            while (DateTime.UtcNow < deadline)
            {
                if (process.HasExited)
                {
                    Assert.Fail("StateForge.Remote.Host exited before gRPC became ready." + Environment.NewLine + GetHostOutput(output));
                }

                try
                {
                    using (GrpcChannel channel = CreateChannel(port, certificateThumbprint, bearerToken))
                    {
                        var client = new StateForgeStoreRpc.StateForgeStoreRpcClient(channel);
                        var store = CreateStore(client, port, bearerToken);
                        string key = "__stateforge_remote_ready_" + Guid.NewGuid().ToString("N");

                        store.Set(key, new byte[] { 1 }, TimeSpan.FromMinutes(1));
                        store.Remove(key);
                        return;
                    }
                }
                catch (RpcException ex)
                {
                    lastException = ex;
                }

                Thread.Sleep(250);
            }

            Assert.Fail(
                "Timed out waiting for StateForge.Remote.Host gRPC readiness. Last error: " +
                (lastException == null ? "<none>" : lastException.Message) +
                Environment.NewLine +
                GetHostOutput(output));
        }

        private static GrpcChannel CreateChannel(int port, string certificateThumbprint, string bearerToken)
        {
            var innerHandler = new SocketsHttpHandler();
            innerHandler.SslOptions = new SslClientAuthenticationOptions
            {
                EnabledSslProtocols = SslProtocols.Tls12,
                RemoteCertificateValidationCallback = delegate(
                    object sender,
                    X509Certificate certificate,
                    X509Chain chain,
                    SslPolicyErrors errors)
                {
                    return IsExpectedCertificate(certificate, certificateThumbprint);
                }
            };

            var bearerHandler = new BearerTokenHandler(bearerToken, innerHandler);
            string address = "https://127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture);
            return GrpcChannel.ForAddress(address, new GrpcChannelOptions { HttpHandler = bearerHandler });
        }

        private static RemoteStateForgeStore CreateStore(
            StateForgeStoreRpc.StateForgeStoreRpcClient client,
            int port,
            string bearerToken)
        {
            return new RemoteStateForgeStore(
                client,
                Options.Create(new RemoteStateForgeOptions
                {
                    Endpoint = "tcp:127.0.0.1:" + port.ToString(CultureInfo.InvariantCulture),
                    BearerToken = bearerToken,
                    CallTimeout = TimeSpan.FromSeconds(10)
                }));
        }

        private static bool IsExpectedCertificate(X509Certificate certificate, string certificateThumbprint)
        {
            if (certificate == null)
            {
                return false;
            }

            using (var certificate2 = new X509Certificate2(certificate))
            {
                return string.Equals(
                    certificate2.GetCertHashString(),
                    certificateThumbprint,
                    StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string CreateCertificate(string certificatePath, string password)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(certificatePath));

            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                    "CN=localhost",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
                request.CertificateExtensions.Add(new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    false));
                request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
                    new OidCollection
                    {
                        new Oid("1.3.6.1.5.5.7.3.1")
                    },
                    false));

                var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
                subjectAlternativeNames.AddDnsName("localhost");
                subjectAlternativeNames.AddIpAddress(IPAddress.Loopback);
                request.CertificateExtensions.Add(subjectAlternativeNames.Build());

                using (X509Certificate2 certificate = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(7)))
                {
                    byte[] pfx = certificate.Export(X509ContentType.Pfx, password);
                    using (var persistedCertificate = new X509Certificate2(
                        pfx,
                        password,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.UserKeySet))
                    {
                        File.WriteAllBytes(certificatePath, persistedCertificate.Export(X509ContentType.Pfx, password));
                        return persistedCertificate.GetCertHashString();
                    }
                }
            }
        }

        private static string CreateAesKey()
        {
            byte[] key = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }

            return Convert.ToBase64String(key);
        }

        private static int GetFreeTcpPort()
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        private static string GetHostAssemblyPath()
        {
            string repositoryRoot = FindRepositoryRoot();
            string configuration = GetConfigurationName();
            string hostPath = Path.Combine(
                repositoryRoot,
                "src",
                "StateForge.Remote.Host",
                "bin",
                configuration,
                "net8.0",
                "StateForge.Remote.Host.dll");

            if (!File.Exists(hostPath))
            {
                Assert.Fail("StateForge.Remote.Host assembly was not built at: " + hostPath);
            }

            return hostPath;
        }

        private static string GetConfigurationName()
        {
            string baseDirectory = AppContext.BaseDirectory;
            string releaseMarker = Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar;

            if (baseDirectory.IndexOf(releaseMarker, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "Release";
            }

            return "Debug";
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "StateForge.sln")) &&
                    Directory.Exists(Path.Combine(directory.FullName, "src")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            Assert.Fail("Could not locate the StateForge repository root from " + AppContext.BaseDirectory);
            return AppContext.BaseDirectory;
        }

        private static void StopProcess(Process process)
        {
            if (process == null)
            {
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    process.WaitForExit(5000);
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static string GetHostOutput(StringBuilder output)
        {
            return "StateForge.Remote.Host output:" + Environment.NewLine + output.ToString();
        }

        private sealed class BearerTokenHandler : DelegatingHandler
        {
            private readonly string bearerToken;

            public BearerTokenHandler(string bearerToken, HttpMessageHandler innerHandler)
                : base(innerHandler)
            {
                this.bearerToken = bearerToken;
            }

            protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
                return base.SendAsync(request, cancellationToken);
            }
        }
    }
}
