using System;

namespace Neo.Network.P2P.Payloads
{
    [Flags]
    public enum VersionServices : ulong
    {
        None = 0,
        NodeNetwork = 1 << 0,
        AcceptRelay = 1 << 1,

        FullNode = NodeNetwork | AcceptRelay
    }
}
