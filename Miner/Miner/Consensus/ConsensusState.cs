using System;

namespace AntShares.Miner.Consensus
{
    [Flags]
    internal enum ConsensusState : byte
    {
        Initial = 0x00,
        Primary = 0x01,
    }
}
