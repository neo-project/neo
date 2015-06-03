using System;

namespace AntShares.Core
{
    [Flags]
    public enum RegisterType : byte
    {
        Share = 1 << 0,
        Currency = 1 << 1,
        Token = 1 << 2
    }
}
