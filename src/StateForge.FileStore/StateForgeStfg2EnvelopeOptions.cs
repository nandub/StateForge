namespace StateForge.FileStore
{
    public sealed class StateForgeStfg2EnvelopeOptions
    {
        public bool UseStfg2Envelope { get; set; }

        public string KeyId { get; set; }

        public StateForgeStfg2EnvelopeOptions()
        {
            UseStfg2Envelope = false;
            KeyId = string.Empty;
        }
    }
}
