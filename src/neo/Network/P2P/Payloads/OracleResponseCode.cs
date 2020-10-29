namespace Neo.Network.P2P.Payloads
{
    public enum OracleResponseCode : byte
    {
        Success = 0x00,

        NotFound = 0x10,
        Timeout = 0x12,
        Forbidden = 0x14,
        InsufficientFunds = 0x16,

        Error = 0xff
    }
}
