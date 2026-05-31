namespace StateForge.Core
{
    public sealed class StateForgeCleanupResult
    {
        public int ExpiredDeleted { get; set; }
        public int InvalidQuarantined { get; set; }
        public int InvalidDeleted { get; set; }
        public int Failed { get; set; }
    }
}
