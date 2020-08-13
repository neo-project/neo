using System;

namespace Neo.SmartContract
{
    [Flags]
    public enum WitnessFlag : byte
    {
        None = 0,

        StateIndependent = 0b00000001,
        StateDependent = 0b00000010,

        All = StateIndependent | StateDependent
    }
}
