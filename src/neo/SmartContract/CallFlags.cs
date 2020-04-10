using System;

namespace Neo.SmartContract
{
    [Flags]
    public enum CallFlags : byte
    {
        None = 0,

        AllowStates = 0b00000001,
        /// <summary>
        /// AllowModifyStates include AllowStates
        /// </summary>
        AllowModifyStates = 0b00000010,
        AllowCall = 0b00000100,
        AllowNotify = 0b00001000,

        ReadOnly = AllowCall | AllowNotify | AllowStates,
        All = AllowStates | AllowModifyStates | AllowCall | AllowNotify
    }
}
