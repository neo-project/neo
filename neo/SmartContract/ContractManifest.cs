using Neo.Ledger;
using System.Linq;

namespace Neo.SmartContract
{
    public class ContractManifest
    {
        public UInt160 Hash { get; set; }
        public ContractManifestGroup Group { get; set; }
        public ContractPropertyState Features { get; set; }
        public ContractAbi Abi { get; set; }
        public ContractPermission[] Permissions { get; set; }
        public UInt160[] Trusts { get; set; }
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

            if (Permissions == null || Permissions.Any(u => u.IsAllowed(manifest.Hash, method)))
            {
                // null == * wildcard

                return true;
            }

            return false;
        }
    }
}