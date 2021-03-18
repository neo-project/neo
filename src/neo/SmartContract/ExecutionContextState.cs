using Neo.VM;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the custom state in <see cref="ExecutionContext"/>.
    /// </summary>
    public class ExecutionContextState
    {
        /// <summary>
        /// The script hash of the current context.
        /// </summary>
        public UInt160 ScriptHash { get; set; }

        /// <summary>
        /// The script hash of the calling contract.
        /// </summary>
        public UInt160 CallingScriptHash { get; set; }

        /// <summary>
        /// The <see cref="ContractState"/> of the current context.
        /// </summary>
        public ContractState Contract { get; set; }

        /// <summary>
        /// The <see cref="SmartContract.CallFlags"/> of the current context.
        /// </summary>
        public CallFlags CallFlags { get; set; } = CallFlags.All;
    }
}
