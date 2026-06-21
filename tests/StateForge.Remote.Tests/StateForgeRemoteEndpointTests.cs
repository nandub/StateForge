using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StateForge.Remote;

namespace StateForge.Remote.Tests
{
    [TestClass]
    public sealed class StateForgeRemoteEndpointTests
    {
        [DataTestMethod]
        [DataRow("tcp:127.0.0.1:7443", "https://127.0.0.1:7443/")]
        [DataRow("tcp:stateforge.internal:7443", "https://stateforge.internal:7443/")]
        [DataRow("https://stateforge.internal:7443", "https://stateforge.internal:7443/")]
        public void ToGrpcAddressAcceptsSecureEndpointForms(string input, string expected)
        {
            Uri actual = StateForgeRemoteEndpoint.ToGrpcAddress(input);

            Assert.AreEqual(expected, actual.ToString());
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("http://stateforge.internal:7443")]
        [DataRow("tcp:")]
        [DataRow("tcp:127.0.0.1")]
        [DataRow("tcp:0.0.0.0:7443")]
        [DataRow("tcp:*:7443")]
        [DataRow("tcp:127.0.0.1:notaport")]
        public void ToGrpcAddressRejectsInvalidOrInsecureEndpointForms(string input)
        {
            Assert.ThrowsException<ArgumentException>(() => StateForgeRemoteEndpoint.ToGrpcAddress(input));
        }
    }
}
