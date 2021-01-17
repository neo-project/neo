namespace Neo.Network.P2P.Payloads
{
    public enum OracleResponseCode : byte
    {
        Success = 0x00,

        ProtocolNotSupported = 0x10,
        ConsensusUnreachable = 0x12,
        NotFound = 0x14,
        Timeout = 0x16,
        Forbidden = 0x18,
        ResponseTooLarge = 0x1a,
        InsufficientFunds = 0x1c,

        Error = 0xff
    }
}
