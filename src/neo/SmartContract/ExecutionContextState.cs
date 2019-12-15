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
        /// Allow call another contract
        /// </summary>
        public bool AllowCall { get; set; } = true;

        /// <summary>
        /// Allow to modify the state, False is the same as ReadOnly mode
        /// </summary>
        public bool AllowModifyStates { get; set; } = true;
    }
}
