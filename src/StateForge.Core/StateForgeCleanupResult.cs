namespace StateForge.Core
{
    /// <summary>Reports the outcomes of an expired-entry cleanup operation.</summary>
    public sealed class StateForgeCleanupResult
    {
        /// <summary>Gets or sets the number of expired entries deleted.</summary>
        public int ExpiredDeleted { get; set; }
        /// <summary>Gets or sets the number of invalid records moved to quarantine.</summary>
        public int InvalidQuarantined { get; set; }
        /// <summary>Gets or sets the number of invalid records deleted.</summary>
        public int InvalidDeleted { get; set; }
        /// <summary>Gets or sets the number of records that could not be processed.</summary>
        public int Failed { get; set; }
    }
}
