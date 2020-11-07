namespace Neo.Network.P2P.Payloads
{
    public enum OracleResponseCode : byte
    {
        Success = 0x00,

        ConsensusUnreachable = 0x10,
        NotFound = 0x12,
        Timeout = 0x14,
        Forbidden = 0x16,
        ResponseTooLarge = 0x18,
        InsufficientFunds = 0x1a,

        Error = 0xff
    }
}
