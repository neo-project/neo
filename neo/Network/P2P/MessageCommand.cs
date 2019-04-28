namespace Neo.Network.P2P
{
    public enum MessageCommand : byte
    {
        // Same value as InventoryType
        Tx = 0x01,
        // Same value as InventoryType
        Block = 0x02,
        Mempool = 0x03,
        Addr = 0x04,
        Inv = 0x05,
        Headers = 0x06,
        MerklebBock = 0x07,
        Version = 0x08,
        Verack = 0x09,
        Alert = 0x0A,
        Reject = 0x0B,

        Ping = 0x10,
        Pong = 0x11,

        GetAddr = 0x20,
        GetBlocks = 0x21,
        GetData = 0x22,
        GetHeaders = 0x23,

        FilterAdd = 0x30,
        FilterClear = 0x31,
        FilterLoad = 0x32,

        // Same value as InventoryType
        Consensus = 0xE0,

        NotFound = 0xFF,
    }
}