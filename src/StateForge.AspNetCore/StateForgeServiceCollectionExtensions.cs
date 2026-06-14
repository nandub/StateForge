using System;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using StateForge.Core;
using StateForge.FileStore;

namespace StateForge.AspNetCore
{
    /// <summary>Registers StateForge implementations with an ASP.NET Core service collection.</summary>
    public static class StateForgeServiceCollectionExtensions
    {
        /// <summary>Registers StateForge as the application <see cref="IDistributedCache"/> implementation.</summary>
        /// <param name="services">The application service collection.</param>
        /// <param name="configure">An action that configures the file-backed distributed cache.</param>
        /// <returns><paramref name="services"/> for chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
        /// <example>
        /// Register StateForge before adding ASP.NET Core session services:
        /// <code language="csharp">
        /// builder.Services.AddStateForgeDistributedCache(options =>
        /// {
        ///     options.RootPath = Path.Combine(
        ///         builder.Environment.ContentRootPath,
        ///         "App_Data",
        ///         "StateForge");
        ///     options.DefaultExpirationMinutes = 20;
        ///     options.EnableCompression = true;
        ///     options.KeepBackups = false;
        /// });
        ///
        /// builder.Services.AddSession();
        /// </code>
        /// </example>
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
