namespace StateForge.Security
{
    public static class StateForgeKeyRingCryptoPlan
    {
        public const string PlannedFileFormatVersion = "STFG2";

        public const string CurrentBehavior = "Current StateForge file encryption remains single-key AES.";

        public const string NextBehavior = "Future STFG2 records should embed KeyId and decrypt through the key ring.";
    }
}
