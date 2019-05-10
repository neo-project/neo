#pragma warning disable CS0612

using Neo.IO.Caching;

namespace Neo.Network.P2P.Payloads
{
    public enum TransactionType : byte
    {
        [ReflectionCache(typeof(RegisterTransaction))]
        RegisterTransaction = 0x40,
        [ReflectionCache(typeof(InvocationTransaction))]
        InvocationTransaction = 0xd1
    }
}
