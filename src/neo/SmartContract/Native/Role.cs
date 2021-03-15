namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents the roles in the NEO system.
    /// </summary>
    public enum Role : byte
    {
        /// <summary>
        /// The validator of state. Used to generate and sign the state root.
        /// </summary>
        StateValidator = 4,

        /// <summary>
        /// The node work for the Oracle service.
        /// </summary>
        Oracle = 8,

        /// <summary>
        /// NeoFS Alphabet node.
        /// </summary>
        NeoFSAlphabetNode = 16
    }
}
