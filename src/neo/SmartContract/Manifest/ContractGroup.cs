using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using System;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// A group represents a set of mutually trusted contracts. A contract will trust and allow any contract in the same group to invoke it, and the user interface will not give any warnings.
    /// A group is identified by a public key and must be accompanied by a signature for the contract hash to prove that the contract is indeed included in the group.
    /// </summary>
    public class ContractGroup
    {
        /// <summary>
        /// Pubkey represents the public key of the group.
        /// </summary>
        public ECPoint PubKey { get; set; }

        /// <summary>
        /// Signature is the signature of the contract hash.
        /// </summary>
        public byte[] Signature { get; set; }

        public ContractGroup Clone()
        {
            return new ContractGroup
            {
                PubKey = PubKey,
                Signature = Signature
            };
        }

        /// <summary>
        /// Parse ContractManifestGroup from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractManifestGroup</returns>
        public static ContractGroup FromJson(JObject json)
        {
            return new ContractGroup
            {
                PubKey = ECPoint.Parse(json["pubkey"].AsString(), ECCurve.Secp256r1),
                Signature = Convert.FromBase64String(json["signature"].AsString()),
            };
        }

        /// <summary>
        /// Return true if the signature is valid
        /// </summary>
        /// <param name="hash">Contract Hash</param>
        /// <returns>Return true or false</returns>
        public bool IsValid(UInt160 hash)
        {
            return Crypto.VerifySignature(hash.ToArray(), Signature, PubKey);
        }

        public virtual JObject ToJson()
        {
            var json = new JObject();
            json["pubkey"] = PubKey.ToString();
            json["signature"] = Convert.ToBase64String(Signature);
            return json;
        }
    }
}
