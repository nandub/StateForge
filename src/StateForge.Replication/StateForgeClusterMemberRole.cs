namespace StateForge.Replication
{
    /// <summary>Defines the supported state forge cluster member role values.</summary>
    public enum StateForgeClusterMemberRole
    {
        /// <summary>Specifies primary.</summary>
        Primary = 0,
        /// <summary>Specifies replica.</summary>
        Replica = 1,
        /// <summary>Specifies witness.</summary>
        Witness = 2
    }
}
