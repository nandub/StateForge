namespace StateForge.Security
{
    /// <summary>Associates an opaque encrypted payload with the key required to process it.</summary>
    public sealed class StateForgeKeyedPayload
    {
        /// <summary>Gets or sets the encryption key identifier.</summary>
        public string KeyId { get; set; }

        /// <summary>Gets or sets the opaque payload bytes.</summary>
        public byte[] Payload { get; set; }
    }
}
