namespace AntShares.Consensus
{
    internal enum ConsensusMessageType : byte
    {
        ChangeView = 0x00,
        PerpareRequest = 0x20,
        PerpareResponse = 0x21,
    }
}
