namespace StateForge.Snapshots
{
    public sealed class StateForgeFailoverResult
    {
        public bool Success { get; set; }

        public bool PrimaryHealthy { get; set; }

        public string PromotedReplicaRootPath { get; set; }

        public string MarkerPath { get; set; }

        public int Errors { get; set; }
    }
}
