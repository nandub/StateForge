using System;
using System.Net;

namespace StateForge.Remote
{
    /// <summary>
    /// Parses StateForge remote endpoint aliases such as <c>tcp:10.0.0.20:7443</c>.
    /// </summary>
    public static class StateForgeRemoteEndpoint
    {
        /// <summary>Converts a StateForge endpoint string to a gRPC HTTPS address.</summary>
        /// <param name="endpoint">Endpoint in <c>tcp:HOST:PORT</c> or <c>https://HOST:PORT</c> form.</param>
        /// <returns>An HTTPS URI suitable for gRPC channel creation.</returns>
        public static Uri ToGrpcAddress(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Remote endpoint is required.", "endpoint");
            }

            endpoint = endpoint.Trim();

            if (endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Uri httpsUri = new Uri(endpoint, UriKind.Absolute);
                ValidateHostAndPort(httpsUri.Host, httpsUri.Port);
                return httpsUri;
            }

            if (!endpoint.StartsWith("tcp:", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Endpoint must use tcp:HOST:PORT or https://HOST:PORT.", "endpoint");
            }

            string authority = endpoint.Substring("tcp:".Length);
            int separator = authority.LastIndexOf(':');

            if (separator <= 0 || separator == authority.Length - 1)
            {
                throw new ArgumentException("Endpoint must use tcp:HOST:PORT.", "endpoint");
            }

            string host = authority.Substring(0, separator);
            string portText = authority.Substring(separator + 1);
            int port;

            if (!int.TryParse(portText, out port))
            {
                throw new ArgumentException("Endpoint port must be numeric.", "endpoint");
            }

            ValidateHostAndPort(host, port);
            return new UriBuilder(Uri.UriSchemeHttps, host, port).Uri;
        }

        private static void ValidateHostAndPort(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                throw new ArgumentException("Endpoint host is required.");
            }

            if (string.Equals(host, "*", StringComparison.Ordinal) ||
                string.Equals(host, "0.0.0.0", StringComparison.Ordinal) ||
                string.Equals(host, "[::]", StringComparison.Ordinal))
            {
                throw new ArgumentException("Client endpoint must target a concrete host or IP address.");
            }

            if (port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
            {
                throw new ArgumentException("Endpoint port is outside the valid TCP port range.");
            }
        }
    }
}
