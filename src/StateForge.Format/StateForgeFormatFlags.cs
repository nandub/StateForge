using System;

namespace StateForge.Format
{
    /// <summary>Identifies transformations applied to an STFG2 payload.</summary>
    [Flags]
    public enum StateForgeFormatFlags
    {
        /// <summary>No payload transformation is declared.</summary>
        None = 0,
        /// <summary>The payload is compressed.</summary>
        Compressed = 1,
        /// <summary>The payload is encrypted.</summary>
        Encrypted = 2,
        /// <summary>The encryption algorithm is AES.</summary>
        Aes = 4,
        /// <summary>The payload is protected with Windows Data Protection API.</summary>
        Dpapi = 8
    }
}
