namespace Neo.Consensus
{
    internal enum ConsensusMessageType : byte
    {
        ChangeView = 0x00,
        PrepareRequest = 0x20,
        PrepareResponse = 0x21,
    }
}
