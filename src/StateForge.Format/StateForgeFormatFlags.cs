using System;

namespace StateForge.Format
{
    [Flags]
    public enum StateForgeFormatFlags
    {
        None = 0,
        Compressed = 1,
        Encrypted = 2,
        Aes = 4,
        Dpapi = 8
    }
}
