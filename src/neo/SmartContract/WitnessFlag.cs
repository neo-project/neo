using System;

namespace Neo.SmartContract
{
    [Flags]
    public enum WitnessFlag : byte
    {
        None = 0,

        StandardWitness = 0b00000001,
        NonStandardWitness = 0b00000010,

        All = StandardWitness | NonStandardWitness
    }
}
