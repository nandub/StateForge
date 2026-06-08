namespace StateForge.FileStore
{
    public sealed class StateForgeStfg2EnvelopeResult
    {
        public bool IsStfg2 { get; set; }

        public bool ChecksumValid { get; set; }

        public string KeyId { get; set; }

        public byte[] Payload { get; set; }

        public string Flags { get; set; }
    }
}
