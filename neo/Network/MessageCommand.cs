namespace Neo.Network
{
    public enum MessageCommand : byte
    {
        addr = 0,
        alert = 1,
        block = 2,
        consensus = 3,
        filteradd = 4,
        filterclear = 5,
        filterload = 6,
        getaddr = 7,
        getblocks = 8,
        getdata = 9,
        getheaders = 10,
        headers = 11,
        mempool = 12,
        merkleblock = 13,
        notfound = 14,
        ping = 15,
        pong = 16,
        reject = 17,
        tx = 18,
        verack = 19,
        version = 20,
        inv = 21,
    }
}