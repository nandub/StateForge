using System.Collections.Generic;

namespace StateForge.Core
{
    public interface IStateForgeAdminStore
    {
        IEnumerable<StateForgeEntryInfo> Enumerate();

        StateForgeStoreDiagnostics GetDiagnostics();

        StateForgeCleanupResult CleanupExpired(bool quarantineInvalid);

        bool ForceRemove(string key);

        StateForgeStoreStats GetStats();

        StateForgeValidationResult ValidateConfiguration();

        StateForgeHealthResult CheckHealth();
    }
}
