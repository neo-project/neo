// Copyright (C) 2015-2025 The Neo Project.
//
// ContractGroup.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Text.Json.Nodes;

namespace Neo.SmartContract.Manifest
{
    /// <summary>
    /// Represents a set of mutually trusted contracts.
    /// A contract will trust and allow any contract in the same group to invoke it, and the user interface will not give any warnings.
    /// A group is identified by a public key and must be accompanied by a signature for the contract hash to prove that the contract is indeed included in the group.
    /// </summary>
    public class ContractGroup : IInteroperable
    {
        /// <summary>
        /// The public key of the group.
        /// </summary>
        public ECPoint PubKey { get; set; }

        /// <summary>
        /// The signature of the contract hash which can be verified by <see cref="PubKey"/>.
        /// </summary>
        public byte[] Signature { get; set; }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            PubKey = ECPoint.DecodePoint(@struct[0].GetSpan(), ECCurve.Secp256r1);
            Signature = @struct[1].GetSpan().ToArray();
        }

        public StackItem ToStackItem(IReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { PubKey.ToArray(), Signature };
        }

        /// <summary>
        /// Converts the group from a JSON object.
        /// </summary>
        /// <param name="json">The group represented by a JSON object.</param>
        /// <returns>The converted group.</returns>
        public static ContractGroup FromJson(JsonObject json)
        {
            ContractGroup group = new()
            {
                PubKey = ECPoint.Parse(json["pubkey"].GetValue<string>(), ECCurve.Secp256r1),
                Signature = Convert.FromBase64String(json["signature"].GetValue<string>()),
            };
            if (group.Signature.Length != 64)
                throw new FormatException($"Signature length({group.Signature.Length}) is not 64");
            return group;
        }

        /// <summary>
        /// Determines whether the signature in the group is valid.
        /// </summary>
        /// <param name="hash">The hash of the contract.</param>
        /// <returns><see langword="true"/> if the signature is valid; otherwise, <see langword="false"/>.</returns>
        public bool IsValid(UInt160 hash)
        {
            return Crypto.VerifySignature(hash.ToArray(), Signature, PubKey);
        }

        /// <summary>
        /// Converts the group to a JSON object.
        /// </summary>
        /// <returns>The group represented by a JSON object.</returns>
        public JsonObject ToJson()
        {
            return new JsonObject()
            {
                ["pubkey"] = PubKey.ToString(),
                ["signature"] = Convert.ToBase64String(Signature)
            };
        }
    }
}
