using Neo.Ledger;

namespace Neo.SmartContract
{
    public class ContractManifest
    {
        public ContractManifestGroup Group { get; set; }
        public ContractPropertyState Features { get; set; }
        public ContractAbi Abi { get; set; }
        public ContractPermission[] Permissions { get; set; }

        /// <summary>
        /// Return true if is allowed
        /// </summary>
        /// <param name="contractHash">Contract hash</param>
        /// <param name="method">Method</param>
        /// <returns>Return true or false</returns>
        public bool IsAllowed(UInt160 contractHash, string method)
        {
            if (Permissions == null) return true; // * wildcard

            foreach (var right in Permissions)
            {
                if (right.IsAllowed(contractHash, method)) return true;
            }

            // TODO: Read Contract manifest group from `contractHash`

            return false;
        }
    }
}