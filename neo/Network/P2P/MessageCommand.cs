namespace Neo.Network.P2P
{
    public enum MessageCommand : byte
    {
        //handshaking
        Version = 0x00,
        Verack = 0x01,

        //connectivity
        GetAddr = 0x10,
        Addr = 0x11,
        Ping = 0x18,
        Pong = 0x19,

        //synchronization
        GetHeaders = 0x20,
        Headers = 0x21,
        GetBlocks = 0x24,
        Mempool = 0x25,
        Inv = 0x27,
        GetData = 0x28,
        NotFound = 0x2a,
        Transaction = 0x2b,
        Block = 0x2c,
        Consensus = 0x2d,
        Reject = 0x2f,

        //SPV protocol
        FilterLoad = 0x30,
        FilterAdd = 0x31,
        FilterClear = 0x32,
        MerkleBlock = 0x38,

        //others
        Alert = 0x40,
    }
}
