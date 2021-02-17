using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// A group represents a set of mutually trusted contracts. A contract will trust and allow any contract in the same group to invoke it, and the user interface will not give any warnings.
    /// A group is identified by a public key and must be accompanied by a signature for the contract hash to prove that the contract is indeed included in the group.
    /// </summary>
    public class ContractGroup : IInteroperable
    {
        /// <summary>
        /// Pubkey represents the public key of the group.
        /// </summary>
        public ECPoint PubKey { get; set; }

        /// <summary>
        /// Signature is the signature of the contract hash.
        /// </summary>
        public byte[] Signature { get; set; }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            PubKey = @struct[0].GetSpan().AsSerializable<ECPoint>();
            Signature = @struct[1].GetSpan().ToArray();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { PubKey.ToArray(), Signature };
        }

        /// <summary>
        /// Parse ContractManifestGroup from json
        /// </summary>
        /// <param name="json">Json</param>
        /// <returns>Return ContractManifestGroup</returns>
        public static ContractGroup FromJson(JObject json)
        {
            ContractGroup group = new ContractGroup
            {
                PubKey = ECPoint.Parse(json["pubkey"].GetString(), ECCurve.Secp256r1),
                Signature = Convert.FromBase64String(json["signature"].GetString()),
            };
            if (group.Signature.Length != 64) throw new FormatException();
            return group;
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
