using System;

namespace Neo.Ledger
{
    [Flags]
    public enum StorageFlags : byte
    {
        None = 0,
        // immutable
        Constant = 0x01,
        // must be of integer type (can be cached for Verification/mempool usage)
        IntegerCache = 0x02
    }
}
