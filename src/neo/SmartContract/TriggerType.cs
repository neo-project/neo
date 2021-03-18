using Neo.Network.P2P.Payloads;
using System;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the triggers for running smart contracts.
    /// </summary>
    [Flags]
    public enum TriggerType : byte
    {
        /// <summary>
        /// Indicate that the contract is triggered by the system to execute the OnPersist method of the native contracts.
        /// </summary>
        OnPersist = 0x01,

        /// <summary>
        /// Indicate that the contract is triggered by the system to execute the PostPersist method of the native contracts.
        /// </summary>
        PostPersist = 0x02,

        /// <summary>
        /// Indicates that the contract is triggered by the verification of a <see cref="IVerifiable"/>.
        /// </summary>
        Verification = 0x20,

        /// <summary>
        /// Indicates that the contract is triggered by the execution of transactions.
        /// </summary>
        Application = 0x40,

        /// <summary>
        /// The combination of all system triggers.
        /// </summary>
        System = OnPersist | PostPersist,

        /// <summary>
        /// The combination of all triggers.
        /// </summary>
        All = OnPersist | PostPersist | Verification | Application
    }
}
