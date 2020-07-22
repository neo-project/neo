using Neo.IO.Caching;

namespace Neo.Network.P2P.Payloads
{
    public enum TransactionAttributeType : byte
    {
        [ReflectionCache(typeof(OracleRequestAttribute))]
        OracleRequest = 0x10,

        [ReflectionCache(typeof(OracleResponseAttribute))]
        OracleResponse = 0x11
    }
}
