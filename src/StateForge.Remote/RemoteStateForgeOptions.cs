using System;

namespace StateForge.Remote
{
    /// <summary>Client settings for the remote StateForge store.</summary>
    public sealed class RemoteStateForgeOptions
    {
        /// <summary>Gets or sets the remote endpoint in <c>tcp:HOST:PORT</c> or <c>https://HOST:PORT</c> form.</summary>
        public string Endpoint { get; set; }

        /// <summary>Gets or sets the optional bearer token used for service-to-service authorization.</summary>
        public string BearerToken { get; set; }

        /// <summary>Gets or sets the per-call deadline.</summary>
        public TimeSpan CallTimeout { get; set; }

        /// <summary>Initializes default remote store options.</summary>
        public RemoteStateForgeOptions()
        {
            Endpoint = string.Empty;
            CallTimeout = TimeSpan.FromSeconds(5);
        }
    }
}
