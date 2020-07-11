using Neo.IO.Caching;

namespace Neo.Network.P2P.Payloads
{
    public enum TransactionAttributeType : byte
    {
        [ReflectionCache(typeof(OracleResponse))]
        OracleResponse = 0x11
    }
}
