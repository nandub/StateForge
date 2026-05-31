namespace StateForge.Core
{
    public class StateForgeOptions
    {
        public string RootPath { get; set; }
        public int StaleLockMinutes { get; set; }
        public int ShardDepth { get; set; }
        public bool EnableCompression { get; set; }
        public bool EnableEncryption { get; set; }

        public StateForgeOptions()
        {
            StaleLockMinutes = 5;
            ShardDepth = 1;
        }
    }
}
