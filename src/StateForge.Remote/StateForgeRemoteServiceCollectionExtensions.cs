using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StateForge.Core;
using StateForge.Remote.Protocol;

namespace StateForge.Remote
{
    /// <summary>Dependency injection helpers for the remote StateForge store.</summary>
    public static class StateForgeRemoteServiceCollectionExtensions
    {
        /// <summary>Registers an <see cref="IStateForgeStore"/> backed by a secure remote gRPC endpoint.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The remote store configuration callback.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection AddRemoteStateForgeStore(
            this IServiceCollection services,
            Action<RemoteStateForgeOptions> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException("services");
            }

            if (configure == null)
            {
                throw new ArgumentNullException("configure");
            }

            services.Configure(configure);

            services.AddGrpcClient<StateForgeStoreRpc.StateForgeStoreRpcClient>((provider, options) =>
            {
                RemoteStateForgeOptions remoteOptions =
                    provider.GetRequiredService<IOptions<RemoteStateForgeOptions>>().Value;

                options.Address = StateForgeRemoteEndpoint.ToGrpcAddress(remoteOptions.Endpoint);
            })
            .AddCallCredentials((context, metadata, provider) =>
            {
                RemoteStateForgeOptions remoteOptions =
                    provider.GetRequiredService<IOptions<RemoteStateForgeOptions>>().Value;

                if (!string.IsNullOrWhiteSpace(remoteOptions.BearerToken))
                {
                    metadata.Add("Authorization", "Bearer " + remoteOptions.BearerToken);
                }

                return Task.CompletedTask;
            });

            services.AddSingleton<IStateForgeStore, RemoteStateForgeStore>();
            return services;
        }
    }
}
