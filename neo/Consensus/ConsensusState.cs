using System;

namespace Neo.Consensus
{
    [Flags]
    internal enum ConsensusState : byte
    {
        Initial = 0x00,
        Primary = 0x01,
        Backup = 0x02,
        RequestSent = 0x04,
        RequestReceived = 0x08,
        SignatureSent = 0x10,
        BlockSent = 0x20,
        ViewChanging = 0x40,
    }
}
