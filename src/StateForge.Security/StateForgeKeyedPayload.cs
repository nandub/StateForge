namespace StateForge.Security
{
    public sealed class StateForgeKeyedPayload
    {
        public string KeyId { get; set; }

        public byte[] Payload { get; set; }
    }
}
