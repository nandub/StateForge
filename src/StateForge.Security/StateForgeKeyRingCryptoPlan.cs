namespace StateForge.Security
{
    /// <summary>Exposes compatibility notes for the transition from single-key records to keyed STFG2 records.</summary>
    public static class StateForgeKeyRingCryptoPlan
    {
        /// <summary>The planned keyed-record format.</summary>
        public const string PlannedFileFormatVersion = "STFG2";

        /// <summary>A description of the current encryption behavior.</summary>
        public const string CurrentBehavior = "Current StateForge file encryption remains single-key AES.";

        /// <summary>A description of the planned keyed-record behavior.</summary>
        public const string NextBehavior = "Future STFG2 records should embed KeyId and decrypt through the key ring.";
    }
}
