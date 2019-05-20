namespace Neo.SmartContract
{
    /// <summary>
    /// The features field describes what features are available for the contract.
    /// </summary>
    public class ContractManifestFeatures
    {
        /// <summary>
        /// The storage field is a boolean value indicating whether the contract has a storage. The value true means that it has, and false means not.
        /// </summary>
        public bool Storage { get; set; }

        /// <summary>
        /// The payable field is a boolean value indicating whether the contract accepts transfers.The value true means that it accepts, and false means not.
        /// </summary>
        public bool Payable { get; set; }
    }
}