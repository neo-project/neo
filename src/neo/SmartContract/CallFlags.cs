using System;

namespace Neo.SmartContract
{
    [Flags]
    public enum CallFlags : byte
    {
        None = 0,
        AllowModifyStates = 1 << 0,
        AllowCall = 1 << 1,

        All = AllowModifyStates | AllowCall
    }
}
