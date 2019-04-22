using System;

namespace Neo.Network.P2P.Payloads
{
    [Flags]
    public enum VersionServices : ulong
    {
        None = 0x00,
        NodeNetwork = 0x01,
    }
}