using System;

namespace Neo.Ledger
{
    [Flags]
    public enum StorageFlags : byte
    {
        None = 0,
        Constant = 0x01
    }
}
