using System;

namespace Neo.SmartContract
{
    [Flags]
    public enum CounterScope : byte
    {
        //Flags

        Shared = 0b00000001,
        Persistent = 0b00000010,

        //Combinations

        Temporary = 0,
        TemporaryShared = Shared,
        Contract = Persistent,
        Global = Shared | Persistent
    }
}
