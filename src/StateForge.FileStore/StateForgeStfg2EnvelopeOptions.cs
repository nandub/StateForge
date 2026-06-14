namespace StateForge.FileStore
{
    /// <summary>Configures optional STFG2 wrapping for file-store workflows.</summary>
    public sealed class StateForgeStfg2EnvelopeOptions
    {
        /// <summary>Gets or sets a value indicating whether STFG2 wrapping is enabled.</summary>
        public bool UseStfg2Envelope { get; set; }

        /// <summary>Gets or sets the key identifier written to new envelopes.</summary>
        public string KeyId { get; set; }

        /// <summary>Initializes disabled envelope options with an empty key identifier.</summary>
        public StateForgeStfg2EnvelopeOptions()
        {
            UseStfg2Envelope = false;
            KeyId = string.Empty;
        }
    }
}
