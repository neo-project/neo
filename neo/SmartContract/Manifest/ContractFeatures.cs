using System;

namespace Neo.SmartContract.Manifest
{
    [Flags]
    public enum ContractFeatures : byte
    {
        NoProperty = 0,

        HasStorage = 1 << 0,
        Payable = 1 << 2
    }
}
