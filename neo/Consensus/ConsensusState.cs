using System;

namespace Neo.Consensus
{
    [Flags]
    public enum ConsensusState : byte
    {
        Initial = 0x00,
        Primary = 0x01,
        Backup = 0x02,
        RequestSent = 0x04,
        RequestReceived = 0x08,
        ResponseSent = 0x10,
        CommitSent = 0x20,
        BlockSent = 0x40,
        ViewChanging = 0x80,
    }
}
