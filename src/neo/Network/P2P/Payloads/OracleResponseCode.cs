namespace Neo.Network.P2P.Payloads
{
    public enum OracleResponseCode : byte
    {
        Success = 0x00,

        NotFound = 0x10,
        Timeout = 0x12,
        Forbidden = 0x14,
        ResponseTooLarge = 0x16,
        InsufficientFunds = 0x18,
        ConsensusUnreachable = 0x1a,

        Error = 0xff
    }
}
