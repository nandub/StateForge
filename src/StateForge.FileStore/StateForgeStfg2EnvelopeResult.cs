namespace StateForge.FileStore
{
    /// <summary>Reports the outcome of inspecting or unwrapping potential STFG2 record bytes.</summary>
    public sealed class StateForgeStfg2EnvelopeResult
    {
        /// <summary>Gets or sets a value indicating whether the input used the STFG2 envelope.</summary>
        public bool IsStfg2 { get; set; }

        /// <summary>Gets or sets a value indicating whether the STFG2 payload checksum matched.</summary>
        public bool ChecksumValid { get; set; }

        /// <summary>Gets or sets the key identifier stored in the envelope.</summary>
        public string KeyId { get; set; }

        /// <summary>Gets or sets the opaque envelope payload or unchanged legacy bytes.</summary>
        public byte[] Payload { get; set; }

        /// <summary>Gets or sets the textual representation of the STFG2 format flags.</summary>
        public string Flags { get; set; }
    }
}
