using Neo.Network.P2P.Payloads;

namespace Neo.Network.P2P
{
    public enum MessageCommand : byte
    {
        //handshaking
        [Payload(typeof(VersionPayload))]
        Version = 0x00,
        Verack = 0x01,

        //connectivity
        GetAddr = 0x10,
        [Payload(typeof(AddrPayload))]
        Addr = 0x11,
        [Payload(typeof(PingPayload))]
        Ping = 0x18,
        [Payload(typeof(PingPayload))]
        Pong = 0x19,

        //synchronization
        [Payload(typeof(GetBlocksPayload))]
        GetHeaders = 0x20,
        [Payload(typeof(HeadersPayload))]
        Headers = 0x21,
        [Payload(typeof(GetBlocksPayload))]
        GetBlocks = 0x24,
        Mempool = 0x25,
        [Payload(typeof(InvPayload))]
        Inv = 0x27,
        [Payload(typeof(InvPayload))]
        GetData = 0x28,
        [Payload(typeof(GetBlockDataPayload))]
        GetBlockData = 0x29,
        NotFound = 0x2a,
        [Payload(typeof(Transaction))]
        Transaction = 0x2b,
        [Payload(typeof(Block))]
        Block = 0x2c,
        [Payload(typeof(ConsensusPayload))]
        Consensus = 0x2d,
        Reject = 0x2f,

        //SPV protocol
        [Payload(typeof(FilterLoadPayload))]
        FilterLoad = 0x30,
        [Payload(typeof(FilterAddPayload))]
        FilterAdd = 0x31,
        FilterClear = 0x32,
        [Payload(typeof(MerkleBlockPayload))]
        MerkleBlock = 0x38,

        //others
        Alert = 0x40,
    }
}
