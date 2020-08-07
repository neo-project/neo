using System;

namespace Neo.SmartContract
{
    [Flags]
    public enum WitnessFlag : byte
    {
        None = 0,
        StateIndependentWitness = 0b00000001,
        StateDependentWitness = 0b00000010,

        All = StateIndependentWitness | StateDependentWitness
    }
}
