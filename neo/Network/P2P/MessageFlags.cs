using System;

namespace Neo.Network.P2P
{
    [Flags]
    public enum MessageFlags : byte
    {
        None = 0,
        Checksum = 1 << 0,
        CompressedGzip = 1 << 1
    }
}