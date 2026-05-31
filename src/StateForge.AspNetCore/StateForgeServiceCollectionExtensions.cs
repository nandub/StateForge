using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using StateForge.Core;
using StateForge.FileStore;

namespace StateForge.AspNetCore
{
    public static class StateForgeServiceCollectionExtensions
    {
        public static IServiceCollection AddStateForgeDistributedCache(this IServiceCollection services, Action<StateForgeDistributedCacheOptions> configure)
        {
            if (services == null) { throw new ArgumentNullException(nameof(services)); }
            if (configure == null) { throw new ArgumentNullException(nameof(configure)); }

            StateForgeDistributedCacheOptions options = new StateForgeDistributedCacheOptions();
            configure(options);

            services.AddSingleton(options);
            services.AddSingleton<StateForgeFileStoreOptions>(options);
            services.AddSingleton<IStateForgeStore, StateForgeFileStore>();
            services.AddSingleton<IDistributedCache, StateForgeDistributedCache>();

            return services;
        }
    }
}
