namespace Neo.Network.P2P.Payloads
{
    public enum WitnessScope : byte
    {
        /// <summary>
        /// (neo2) - no params
        /// </summary>
        Global = 0x00,

        /// <summary>
        /// EntryOnly means that this condition must hold: EntryScriptHash == CallingScriptHash
        /// No params is needed, as the witness/permission/signature given on first invocation will automatically expire if entering deeper internal invokes
        /// This can be default safe choice for native NEO/GAS (previously used on Neo 2 as "attach" mode)
        /// </summary>
        EntryOnly = 0x01,

        /// <summary>
        /// Custom hash for contract-specific
        /// </summary>
        CustomScriptHash = 0x02,

        // 0x03 -> composition between EntryOnly and CustomScriptHash. 
        // Example: we invoke NEO native transfer using 0x01 (it should work).
        // We invoke NEO native transfer using 0x03, and custom hash for GAS asset (it should fail)

        /// <summary>
        ///  Custom pubkey for group members
        /// </summary>
        ExecutingGroupPubKey = 0x04

        // 0x05 -> composition of Group + EntryOnly. See example for 0x03.

        // 0x06 -> composition of Group + Custom. Probably, Group hash should be considered instead of CustomHash.
        // 0x06 should be equivalent to 0x04
        
        // 0x07 -> this should be equivalent to 0x05 (since 0x06 -> 0x04) + 0x01
    }
}
