// Copyright (C) 2015-2025 The Neo Project.
//
// Witness.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents a witness of an <see cref="IVerifiable"/> object.
    /// </summary>
    public class Witness : ISerializable
    {
        // This is designed to allow a MultiSig 21/11 (committee)
        // Invocation = 11 * (64 + 2) = 726
        private const int MaxInvocationScript = 1024;

        // Verification = m + (PUSH_PubKey * 21) + length + null + syscall = 1 + ((2 + 33) * 21) + 2 + 1 + 5 = 744
        private const int MaxVerificationScript = 1024;

        /// <summary>
        /// The invocation script of the witness. Used to pass arguments for <see cref="VerificationScript"/>.
        /// </summary>
        public ReadOnlyMemory<byte> InvocationScript;

        /// <summary>
        /// The verification script of the witness. It can be empty if the contract is deployed.
        /// </summary>
        public ReadOnlyMemory<byte> VerificationScript;

        private UInt160 _scriptHash;
        /// <summary>
        /// The hash of the <see cref="VerificationScript"/>.
        /// </summary>
        public UInt160 ScriptHash
        {
            get
            {
                if (_scriptHash == null)
                {
                    _scriptHash = VerificationScript.Span.ToScriptHash();
                }
                return _scriptHash;
            }
        }

        public int Size => InvocationScript.GetVarSize() + VerificationScript.GetVarSize();

        /// <summary>
        /// Creates a new <see cref="Witness"/> with empty invocation and verification scripts.
        /// </summary>
        public static Witness Empty => new()
        {
            InvocationScript = ReadOnlyMemory<byte>.Empty,
            VerificationScript = ReadOnlyMemory<byte>.Empty,
        };

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            InvocationScript = reader.ReadVarMemory(MaxInvocationScript);
            VerificationScript = reader.ReadVarMemory(MaxVerificationScript);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(InvocationScript.Span);
            writer.WriteVarBytes(VerificationScript.Span);
        }

        /// <summary>
        /// Converts the witness to a JSON object.
        /// </summary>
        /// <returns>The witness represented by a JSON object.</returns>
        public JObject ToJson()
        {
            JObject json = new();
            json["invocation"] = Convert.ToBase64String(InvocationScript.Span);
            json["verification"] = Convert.ToBase64String(VerificationScript.Span);
            return json;
        }

        public Witness Clone()
        {
            return new Witness()
            {
                _scriptHash = _scriptHash,
                InvocationScript = InvocationScript.ToArray(),
                VerificationScript = VerificationScript.ToArray()
            };
        }
    }
}
