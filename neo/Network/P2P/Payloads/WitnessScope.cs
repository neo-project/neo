namespace Neo.Network.P2P.Payloads
{
    public enum WitnessScope : byte
    {
        /// <summary>
        /// (neo2) - no params
        /// </summary>
        Global = 0x00,

        /// <summary>
        /// EntryScriptHash - no params (root-only witnesses) - can be default safe choice for native NEO/GAS (neo2 attach mode)
        /// </summary>
        InitScriptHash = 0x01,

        /// <summary>
        /// Custom hash for contract-specific
        /// </summary>
        CustomScriptHash = 0x02,

        /// <summary>
        ///  Custom pubkey for group members
        /// </summary>
        ExecutingGroupPubKey = 0x03
    }
}