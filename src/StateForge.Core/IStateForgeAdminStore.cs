using System.Collections.Generic;

namespace StateForge.Core
{
    /// <summary>
    /// Provides administrative inspection, validation, cleanup, and health operations for a StateForge store.
    /// </summary>
    public interface IStateForgeAdminStore
    {
        /// <summary>Enumerates metadata for the entries currently visible to the store.</summary>
        /// <returns>A sequence of entry metadata records.</returns>
        IEnumerable<StateForgeEntryInfo> Enumerate();

        /// <summary>Gets filesystem paths and file counts used by the store.</summary>
        /// <returns>The current store diagnostics.</returns>
        StateForgeStoreDiagnostics GetDiagnostics();

        /// <summary>Deletes expired entries and handles invalid records.</summary>
        /// <param name="quarantineInvalid"><see langword="true"/> to quarantine invalid records; <see langword="false"/> to delete them.</param>
        /// <returns>Counts for each cleanup outcome.</returns>
        StateForgeCleanupResult CleanupExpired(bool quarantineInvalid);

        /// <summary>Removes an entry without requiring its lock token.</summary>
        /// <param name="key">The logical entry key.</param>
        /// <returns><see langword="true"/> when an entry was removed; otherwise, <see langword="false"/>.</returns>
        bool ForceRemove(string key);

        /// <summary>Calculates aggregate statistics for the store.</summary>
        /// <returns>The current store statistics.</returns>
        StateForgeStoreStats GetStats();

        /// <summary>Validates the configured store paths and options.</summary>
        /// <returns>Configuration errors and warnings.</returns>
        StateForgeValidationResult ValidateConfiguration();

        /// <summary>Runs read, write, lock, enumeration, and cleanup health probes.</summary>
        /// <returns>The health probe result.</returns>
        StateForgeHealthResult CheckHealth();
    }
}
