using Neo.Cryptography;

namespace Neo.SmartContract
{
    /// <summary>
    /// A group represents a set of mutually trusted contracts. A contract will trust and allow any contract in the same group to invoke it, and the user interface will not give any warnings.
    /// The group field can be null.
    /// A group is identified by a public key and must be accompanied by a signature for the contract hash to prove that the contract is indeed included in the group.
    /// </summary>
    public class ContractManifestGroup
    {
        /// <summary>
        /// Pubkey represents the public key of the group.
        /// </summary>
        public UInt160 PubKey { get; set; }

        /// <summary>
        /// Signature is the signature of the contract hash.
        /// </summary>
        public byte[] Signature { get; set; }

        /// <summary>
        /// Return true if the signature is valid
        /// </summary>
        /// <param name="contractHash">Contract Hash</param>
        /// <returns>Return true or false</returns>
        public bool IsValid(UInt160 contractHash)
        {
            return Crypto.Default.VerifySignature(contractHash.ToArray(), Signature, PubKey.ToArray());
        }
    }
}