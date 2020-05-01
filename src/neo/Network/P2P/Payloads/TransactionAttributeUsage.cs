using Neo.IO.Caching;

namespace Neo.Network.P2P.Payloads
{
    public enum TransactionAttributeUsage : byte
    {
        [ReflectionCache(typeof(CosignerAttribute))]
        Cosigner = 0x81
    }
}
