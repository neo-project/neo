using System;

namespace Neo.Network.P2P.Payloads
{
    [Flags]
    public enum TransactionVersion : byte
    {
        Default = 0,

        All = 0
    }
}
