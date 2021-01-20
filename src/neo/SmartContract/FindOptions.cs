using System;

namespace Neo.SmartContract
{
    [Flags]
    public enum FindOptions : byte
    {
        None = 0,

        KeysOnly = 1 << 0,
        RemovePrefix = 1 << 1,
        ValuesOnly = 1 << 2,
        DeserializeValues = 1 << 3,
        PickField0 = 1 << 4,
        PickField1 = 1 << 5,

        All = KeysOnly | RemovePrefix | ValuesOnly | DeserializeValues | PickField0 | PickField1
    }
}
