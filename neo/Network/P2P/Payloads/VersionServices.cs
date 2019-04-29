using System;

namespace Neo.Network.P2P.Payloads
{
    [Flags]
    public enum VersionServices : ulong
    {
        None = 0,
        NodeNetwork = 1 << 0,
        Relay = 1 << 1,

        FullNode = NodeNetwork | Relay
    }
}