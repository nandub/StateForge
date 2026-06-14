namespace StateForge.Core
{
    /// <summary>Defines constants used by the original StateForge record format.</summary>
    public static class StateForgeConstants
    {
        /// <summary>The little-endian integer representation of the <c>STFG</c> record magic.</summary>
        public const int FileMagic = 0x47465453;
        /// <summary>The original StateForge record-format version.</summary>
        public const int FileVersion = 1;
        /// <summary>Indicates that a record payload is compressed.</summary>
        public const int FlagCompressed = 1;
        /// <summary>Indicates that a record payload is protected by the configured encryption provider.</summary>
        public const int FlagEncrypted = 2;
        /// <summary>Indicates that a record payload is encrypted with AES.</summary>
        public const int FlagAesEncrypted = 4;
        /// <summary>Indicates that an AES record includes an authentication trailer.</summary>
        public const int FlagAuthenticated = 8;
    }
}
