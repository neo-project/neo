using System;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the operations allowed when a contract is called.
    /// </summary>
    [Flags]
    public enum CallFlags : byte
    {
        /// <summary>
        /// No flag is set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the called contract is allowed to read states.
        /// </summary>
        ReadStates = 0b00000001,

        /// <summary>
        /// Indicates that the called contract is allowed to write states.
        /// </summary>
        WriteStates = 0b00000010,

        /// <summary>
        /// Indicates that the called contract is allowed to call another contract.
        /// </summary>
        AllowCall = 0b00000100,

        /// <summary>
        /// Indicates that the called contract is allowed to send notifications.
        /// </summary>
        AllowNotify = 0b00001000,

        /// <summary>
        /// Indicates that the called contract is not allowed to check witnesses.
        /// </summary>
        DisableCheckWitness = 0b00010000,

        /// <summary>
        /// Indicates that the called contract is allowed to read or write states.
        /// </summary>
        States = ReadStates | WriteStates,

        /// <summary>
        /// Indicates that the called contract is allowed to read states or call another contract.
        /// </summary>
        ReadOnly = ReadStates | AllowCall,

        /// <summary>
        /// All flags are set.
        /// </summary>
        All = States | AllowCall | AllowNotify
    }
}
