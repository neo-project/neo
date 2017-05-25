using System;

namespace AntShares.SmartContract
{
    [Flags]
    internal enum StorageContext : byte
    {
        Current = 1,
        CallingContract = 2,
        EntryContract = 4
    }
}
