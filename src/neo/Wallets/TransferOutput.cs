namespace Neo.Wallets
{
    /// <summary>
    /// Represents an output of a transfer.
    /// </summary>
    public class TransferOutput
    {
        /// <summary>
        /// The id of the asset to transfer.
        /// </summary>
        public UInt160 AssetId;

        /// <summary>
        /// The amount of the asset to transfer.
        /// </summary>
        public BigDecimal Value;

        /// <summary>
        /// The account to transfer to.
        /// </summary>
        public UInt160 ScriptHash;

        /// <summary>
        /// The object to be passed to the transfer method of NEP-17.
        /// </summary>
        public object Data;
    }
}
