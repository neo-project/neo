namespace Neo.Network.P2P
{
    public enum MessageCommand : byte
    {
        // Same value as InventoryType
        tx = 0x01,
        // Same value as InventoryType
        block = 0x02,
        mempool = 0x03,
        addr = 0x04,
        inv = 0x05,
        headers = 0x06,
        merkleblock = 0x07,
        version = 0x08,
        verack = 0x09,
        alert = 0x0A,
        reject = 0x0B,

        ping = 0x10,
        pong = 0x11,

        getaddr = 0x20,
        getblocks = 0x21,
        getdata = 0x22,
        getheaders = 0x23,

        filteradd = 0x30,
        filterclear = 0x31,
        filterload = 0x32,

        // Same value as InventoryType
        consensus = 0xE0,

        notfound = 0xFF,
    }
}