using System;

namespace Neo.Network.P2P.Payloads
{
    [Flags]
    public enum TransactionVersion : byte
    {
        Transaction = 0b00000001,
        OracleRequest = 0b00000010,
        OracleResponse = 0b00000100,

        All = Transaction | OracleRequest | OracleResponse
    }
}
