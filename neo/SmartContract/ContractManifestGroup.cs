using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using System;
using System.Linq;

namespace Neo.SmartContract
{
    /// <summary>
    /// A group represents a set of mutually trusted contracts. A contract will trust and allow any contract in the same group to invoke it, and the user interface will not give any warnings.
    /// The group field can be null.
    /// A group is identified by a public key and must be accompanied by a signature for the contract hash to prove that the contract is indeed included in the group.
    /// </summary>
    public class ContractManifestGroup : IEquatable<ContractManifestGroup>
    {
        /// <summary>
        /// Pubkey represents the public key of the group.
        /// </summary>
        public ECPoint PubKey { get; set; }

        /// <summary>
        /// Signature is the signature of the contract hash.
        /// </summary>
        public byte[] Signature { get; set; }

        public bool Equals(ContractManifestGroup other)
        {
            if (other == null) return false;
            if (ReferenceEquals(other, this)) return true;

            if (!PubKey.Equals(other.PubKey)) return false;
            if (!Signature.SequenceEqual(other.Signature)) return false;

            return true;
        }

        /// <summary>
        /// Return true if the signature is valid
        /// </summary>
        /// <param name="contractHash">Contract Hash</param>
        /// <returns>Return true or false</returns>
        public bool IsValid(UInt160 contractHash)
        {
            return Crypto.Default.VerifySignature(contractHash.ToArray(), Signature, PubKey.ToArray());
        }

        /// <summary>
        /// Parse ContractManifestGroup from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractManifestGroup</returns>
        public static ContractManifestGroup Parse(JObject json)
        {
            return new ContractManifestGroup
            {
                PubKey = ECPoint.Parse(json["pubKey"].AsString(), ECCurve.Secp256r1),
                Signature = json["signature"].AsString().HexToBytes(),
            };
        }

        public virtual JObject ToJson()
        {
            var json = new JObject();
            json["pubKey"] = PubKey.ToString();
            json["signature"] = Signature.ToHexString();
            return json;
        }
    }
}