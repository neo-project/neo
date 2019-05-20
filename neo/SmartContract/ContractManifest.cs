using Neo.Ledger;
using System.Linq;

namespace Neo.SmartContract
{
    /// <summary>
    /// When a smart contract is deployed, it must explicitly declare the features and permissions it will use.
    /// When it is running, it will be limited by its declared list of features and permissions, and cannot make any behavior beyond the scope of the list.
    /// </summary>
    public class ContractManifest
    {
        /// <summary>
        /// Contract hash
        /// </summary>
        public UInt160 Hash { get; set; }

        /// <summary>
        /// A group represents a set of mutually trusted contracts. A contract will trust and allow any contract in the same group to invoke it, and the user interface will not give any warnings.
        /// The group field can be null.
        /// </summary>
        public ContractManifestGroup Group { get; set; }

        /// <summary>
        /// The features field describes what features are available for the contract.
        /// </summary>
        public ContractPropertyState Features { get; set; }

        /// <summary>
        /// For technical details of ABI, please refer to NEP-3: NeoContract ABI. (https://github.com/neo-project/proposals/blob/master/nep-3.mediawiki)
        /// </summary>
        public ContractAbi Abi { get; set; }

        /// <summary>
        /// The permissions field is an array containing a set of Permission objects. It describes which contracts may be invoked and which methods are called.
        /// </summary>
        public ContractPermission[] Permissions { get; set; }

        /// <summary>
        /// The trusts field is an array containing a set of contract hashes or group public keys. It can also be assigned with a wildcard *. If it is a wildcard *, then it means that it trusts any contract.
        /// If a contract is trusted, the user interface will not give any warnings when called by the contract.
        /// </summary>
        public UInt160[] Trusts { get; set; }
        
        /// <summary>
        /// The safemethods field is an array containing a set of method names. It can also be assigned with a wildcard *. If it is a wildcard *, then it means that all methods of the contract are safe.
        /// If a method is marked as safe, the user interface will not give any warnings when it is called by any other contract.
        /// </summary>
        public string[] SafeMethods { get; set; }

        /// <summary>
        /// Return true if is allowed
        /// </summary>
        /// <param name="manifest">Manifest</param>
        /// <param name="method">Method</param>
        /// <returns>Return true or false</returns>
        public bool CanCall(ContractManifest manifest, string method)
        {
            if (Group != null && manifest.Group != null && manifest.Group.PubKey.Equals(Group.PubKey))
            {
                // Same group

                return true;
            }

            if (manifest.Trusts != null && manifest.Trusts.Contains(Hash))
            {
                // null == * wildcard
                // You don't have rights in the contract

                return false;
            }

            // null == * wildcard

            return Permissions == null || Permissions.Any(u => u.IsAllowed(manifest.Hash, method));
        }
    }
}