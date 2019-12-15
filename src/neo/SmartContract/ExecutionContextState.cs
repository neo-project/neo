namespace Neo.SmartContract
{
    internal class ExecutionContextState
    {
        /// <summary>
        /// Script hash
        /// </summary>
        public UInt160 ScriptHash { get; set; }

        /// <summary>
        /// Calling script hash
        /// </summary>
        public UInt160 CallingScriptHash { get; set; }

        /// <summary>
        /// Execution context rights
        /// </summary>
        public CallFlags Rights { get; set; }

        /// <summary>
        /// Allow call another contract
        /// </summary>
        public bool AllowCall => Rights.HasFlag(CallFlags.AllowCall);

        /// <summary>
        /// Allow to modify the state, False is the same as ReadOnly mode
        /// </summary>
        public bool AllowModifyStates => Rights.HasFlag(CallFlags.AllowModifyStates);
    }
}
