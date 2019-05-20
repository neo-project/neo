namespace Neo.SmartContract
{
    /// <summary>
    /// For technical details of ABI, please refer to NEP-3: NeoContract ABI. (https://github.com/neo-project/proposals/blob/master/nep-3.mediawiki)
    /// </summary>
    public class ContractAbi
    {
        /// <summary>
        /// Hash is the script hash of the contract. It is encoded as a hexadecimal string in big-endian.
        /// </summary>
        public UInt160 Hash { get; set; }

        /// <summary>
        /// Entrypoint is a Method object which describe the details of the entrypoint of the contract.
        /// </summary>
        public string EntryPoint { get; set; }

        /// <summary>
        /// Methods is an array of Method objects which describe the details of each method in the contract.
        /// </summary>
        public ContractMethodWithReturnDefinition[] Methods { get; set; }

        /// <summary>
        /// Events is an array of Event objects which describe the details of each event in the contract.
        /// </summary>
        public ContractMethodDefinition[] Events { get; set; }
    }
}