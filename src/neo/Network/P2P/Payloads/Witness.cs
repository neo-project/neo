// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory or 
// the project http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.IO.Json;
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
        public byte[] InvocationScript;

        /// <summary>
        /// The verification script of the witness. It can be empty if the contract is deployed.
        /// </summary>
        public byte[] VerificationScript;

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
                    _scriptHash = VerificationScript.ToScriptHash();
                }
                return _scriptHash;
            }
        }

        public int Size => InvocationScript.GetVarSize() + VerificationScript.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            InvocationScript = reader.ReadVarBytes(MaxInvocationScript);
            VerificationScript = reader.ReadVarBytes(MaxVerificationScript);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(InvocationScript);
            writer.WriteVarBytes(VerificationScript);
        }

        /// <summary>
        /// Converts the witness to a JSON object.
        /// </summary>
        /// <returns>The witness represented by a JSON object.</returns>
        public JObject ToJson()
        {
            JObject json = new();
            json["invocation"] = Convert.ToBase64String(InvocationScript);
            json["verification"] = Convert.ToBase64String(VerificationScript);
            return json;
        }
    }
}
