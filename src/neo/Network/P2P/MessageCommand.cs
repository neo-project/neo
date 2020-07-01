using Neo.IO.Caching;
using Neo.Network.P2P.Payloads;

namespace Neo.Network.P2P
{
    public enum MessageCommand : byte
    {
        //handshaking
        [ReflectionCache(typeof(VersionPayload))]
        Version = 0x00,
        Verack = 0x01,

        //connectivity
        GetAddr = 0x10,
        [ReflectionCache(typeof(AddrPayload))]
        Addr = 0x11,
        [ReflectionCache(typeof(PingPayload))]
        Ping = 0x18,
        [ReflectionCache(typeof(PingPayload))]
        Pong = 0x19,

        //synchronization
        [ReflectionCache(typeof(GetBlockByIndexPayload))]
        GetHeaders = 0x20,
        [ReflectionCache(typeof(HeadersPayload))]
        Headers = 0x21,
        [ReflectionCache(typeof(GetBlocksPayload))]
        GetBlocks = 0x24,
        Mempool = 0x25,
        [ReflectionCache(typeof(InvPayload))]
        Inv = 0x27,
        [ReflectionCache(typeof(InvPayload))]
        GetData = 0x28,
        [ReflectionCache(typeof(GetBlockByIndexPayload))]
        GetBlockByIndex = 0x29,
        NotFound = 0x2a,
        [ReflectionCache(typeof(Transaction))]
        Transaction = 0x2b,
        [ReflectionCache(typeof(Block))]
        Block = 0x2c,
        [ReflectionCache(typeof(ConsensusPayload))]
        Consensus = 0x2d,
        Reject = 0x2f,

        //SPV protocol
        [ReflectionCache(typeof(FilterLoadPayload))]
        FilterLoad = 0x30,
        [ReflectionCache(typeof(FilterAddPayload))]
        FilterAdd = 0x31,
        FilterClear = 0x32,
        [ReflectionCache(typeof(MerkleBlockPayload))]
        MerkleBlock = 0x38,

        //others
        Alert = 0x40,
    }
}
