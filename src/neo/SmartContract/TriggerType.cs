using System;

namespace Neo.SmartContract
{
    [Flags]
    public enum TriggerType : byte
    {
        System = 0x01,
        /// <summary>
        /// The verification trigger indicates that the contract is being invoked as a verification function.
        /// The verification function can accept multiple parameters, and should return a boolean value that indicates the validity of the transaction or block.
        /// If want to use the verification function, must implement the `verify` method in contract. 
        /// </summary>
        Verification = 0x20,
        /// <summary>
        /// The application trigger indicates that the contract is being invoked as an application function.
        /// The application function can accept multiple parameters, change the states of the blockchain, and return any type of value.
        /// </summary>
        Application = 0x40,

        All = System | Verification | Application
    }
}
