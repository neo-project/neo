using Neo.Ledger;
using System;
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
        public ContractManifestGroup[] Groups { get; set; }

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
        public WildCardContainer<ContractPermission> Permissions { get; set; }

        /// <summary>
        /// The trusts field is an array containing a set of contract hashes or group public keys. It can also be assigned with a wildcard *. If it is a wildcard *, then it means that it trusts any contract.
        /// If a contract is trusted, the user interface will not give any warnings when called by the contract.
        /// </summary>
        public WildCardContainer<UInt160> Trusts { get; set; }

        /// <summary>
        /// The safemethods field is an array containing a set of method names. It can also be assigned with a wildcard *. If it is a wildcard *, then it means that all methods of the contract are safe.
        /// If a method is marked as safe, the user interface will not give any warnings when it is called by any other contract.
        /// </summary>
        public WildCardContainer<string> SafeMethods { get; set; }

        /// <summary>
        /// Return true if is allowed
        /// </summary>
        /// <param name="manifest">Manifest</param>
        /// <param name="method">Method</param>
        /// <returns>Return true or false</returns>
        public bool CanCall(ContractManifest manifest, string method)
        {
            if (Groups != null && manifest.Groups != null && Groups.Any(a => manifest.Groups.Any(b => a.PubKey.Equals(b.PubKey))))
            {
                // Same group

                return true;
            }

            if (manifest.Trusts != null && !manifest.Trusts.IsWildcard && !manifest.Trusts.Contains(Hash))
            {
                // null == * wildcard
                // You don't have rights in the contract

                return false;
            }

            return Permissions == null || Permissions.IsWildcard || Permissions.Any(u => u.IsAllowed(manifest.Hash, method));
        }

        /// <summary>
        /// Parse ContractManifest from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return Contract manifest</returns>
        public static ContractManifest Parse(string json)
        {
            // TODO: Parse json

            throw new NotImplementedException();
        }

        /// <summary>
        /// To json
        /// </summary>
        /// <returns>Return json string</returns>
        public string ToJson()
        {
            // TODO: Generate json

            throw new NotImplementedException();
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <returns>Return a copy of this object</returns>
        public ContractManifest Clone() => Parse(ToJson());

        /// <summary>
        /// String representation
        /// </summary>
        /// <returns>Return json string</returns>
        public override string ToString() => ToJson();
    }
}