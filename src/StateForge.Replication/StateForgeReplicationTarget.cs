namespace StateForge.Replication
{
    /// <summary>Represents state forge replication target.</summary>
    public sealed class StateForgeReplicationTarget
    {
        /// <summary>Gets or sets the name.</summary>
        public string Name { get; set; }

        /// <summary>Gets or sets the root path.</summary>
        public string RootPath { get; set; }

        /// <summary>Gets or sets the sessions path.</summary>
        public string SessionsPath { get; set; }

        /// <summary>Gets or sets the site name.</summary>
        public string SiteName { get; set; }

        /// <summary>Gets or sets the region.</summary>
        public string Region { get; set; }
    }
}
