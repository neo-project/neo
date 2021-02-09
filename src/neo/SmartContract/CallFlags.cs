using System;

namespace Neo.SmartContract
{
    [Flags]
    public enum CallFlags : byte
    {
        None = 0,

        ReadStates = 0b00000001,
        WriteStates = 0b00000011,
        AllowCall = 0b00000100,
        AllowNotify = 0b00001000,

        ReadOnly = ReadStates | AllowCall,
        All = WriteStates | AllowCall | AllowNotify
    }
}
