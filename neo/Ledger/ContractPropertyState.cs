using System;

namespace Neo.Ledger
{
    [Flags]
    public enum ContractPropertyState : byte
    {
        NoProperty = 0,

        HasStorage = 1 << 0,
        Payable = 1 << 2
    }
}
