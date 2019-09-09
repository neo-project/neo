namespace Neo.SmartContract
{
    public class ExecutionContextState
    {
        /// <summary>
        /// Script hash
        /// </summary>
        public UInt160 ScriptHash { get; set; }

        /// <summary>
        /// Is read only
        /// </summary>
        public bool ReadOnly { get; set; } = false;
    }
}
