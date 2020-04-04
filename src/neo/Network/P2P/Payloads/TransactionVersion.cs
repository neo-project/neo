namespace Neo.Network.P2P.Payloads
{
    public enum TransactionVersion : byte
    {
        Transaction = 0x00,
        OracleRequest = 0x01,
        OracleResponse = 0x02
    }
}
