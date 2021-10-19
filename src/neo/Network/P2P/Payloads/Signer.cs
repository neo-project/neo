// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo.VM.Types;
using Array = System.Array;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents a signer of a <see cref="Transaction"/>.
    /// </summary>
    public class Signer : ISerializable
    {
        // This limits maximum number of AllowedContracts or AllowedGroups here
        private const int MaxSubitems = 16;

        /// <summary>
        /// The account of the signer.
        /// </summary>
        public UInt160 Account;

        /// <summary>
        /// The scopes of the witness.
        /// </summary>
        public WitnessScope Scopes;

        /// <summary>
        /// The contracts that allowed by the witness. Only available when the <see cref="WitnessScope.CustomContracts"/> flag is set.
        /// </summary>
        public UInt160[] AllowedContracts;

        /// <summary>
        /// The groups that allowed by the witness. Only available when the <see cref="WitnessScope.CustomGroups"/> flag is set.
        /// </summary>
        public ECPoint[] AllowedGroups;

        public IDictionary<UInt160, UInt160[]> AllowedCallingContracts;
        public IDictionary<ContractOrGroup, ContractOrGroup[]> AllowedCallingGroup;

        public int Size =>
            /*Account*/             UInt160.Length +
            /*Scopes*/              sizeof(WitnessScope) +
            /*AllowedContracts*/    (Scopes.HasFlag(WitnessScope.CustomContracts) ? AllowedContracts.GetVarSize() : 0) +
            /*AllowedGroups*/       (Scopes.HasFlag(WitnessScope.CustomGroups) ? AllowedGroups.GetVarSize() : 0) +
            /*AllowedCustomCallingContracts*/  (Scopes.HasFlag(WitnessScope.CustomCallingContracts) ? AllowedCallingContracts.GetVarSize() : 0) +
            /*AllowedCustomCallingGroups*/  (Scopes.HasFlag(WitnessScope.CustomCallingGroups) ? AllowedCallingGroup.GetVarSize() : 0);

        public void Deserialize(BinaryReader reader)
        {
            Account = reader.ReadSerializable<UInt160>();
            Scopes = (WitnessScope)reader.ReadByte();
            if ((Scopes & ~(WitnessScope.CalledByEntry | WitnessScope.CustomContracts | WitnessScope.CustomGroups | WitnessScope.CustomCallingContracts | WitnessScope.CustomCallingGroups | WitnessScope.Global)) != 0)
                throw new FormatException();
            if (Scopes.HasFlag(WitnessScope.Global) && Scopes != WitnessScope.Global)
                throw new FormatException();
            AllowedContracts = Scopes.HasFlag(WitnessScope.CustomContracts)
                ? reader.ReadSerializableArray<UInt160>(MaxSubitems)
                : Array.Empty<UInt160>();
            AllowedGroups = Scopes.HasFlag(WitnessScope.CustomGroups)
                ? reader.ReadSerializableArray<ECPoint>(MaxSubitems)
                : Array.Empty<ECPoint>();
            AllowedCallingContracts = Scopes.HasFlag(WitnessScope.CustomCallingContracts)
                ? reader.ReadLookup<UInt160, UInt160>(MaxSubitems)
                : new Dictionary<UInt160, UInt160[]>();
            AllowedCallingGroup = Scopes.HasFlag(WitnessScope.CustomCallingGroups)
                ? reader.ReadLookup<ContractOrGroup, ContractOrGroup>(MaxSubitems)
                : new Dictionary<ContractOrGroup, ContractOrGroup[]>();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Account);
            writer.Write((byte)Scopes);
            if (Scopes.HasFlag(WitnessScope.CustomContracts))
                writer.Write(AllowedContracts);
            if (Scopes.HasFlag(WitnessScope.CustomGroups))
                writer.Write(AllowedGroups);
            if (Scopes.HasFlag(WitnessScope.CustomCallingContracts))
                writer.WriteLookup(AllowedCallingContracts);
            if (Scopes.HasFlag(WitnessScope.CustomCallingGroups))
                writer.WriteLookup(AllowedCallingGroup);
        }

        /// <summary>
        /// Converts the signer to a JSON object.
        /// </summary>
        /// <returns>The signer represented by a JSON object.</returns>
        public JObject ToJson()
        {
            var json = new JObject();
            json["account"] = Account.ToString();
            json["scopes"] = Scopes;
            if (Scopes.HasFlag(WitnessScope.CustomContracts))
                json["allowedcontracts"] = AllowedContracts.Select(p => (JObject)p.ToString()).ToArray();
            if (Scopes.HasFlag(WitnessScope.CustomGroups))
                json["allowedgroups"] = AllowedGroups.Select(p => (JObject)p.ToString()).ToArray();
            if (Scopes.HasFlag(WitnessScope.CustomCallingContracts))
            {
                json["allowedcallingcontracts"] = AllowedCallingContracts.Select(p =>
                {
                    var obj = new JObject();
                    obj["contract"] = p.Key.ToString();
                    obj["trusts"] = p.Value.Select(v => (JObject)v.ToString()).ToArray();
                    return obj;
                }).ToArray();
            }
            if (Scopes.HasFlag(WitnessScope.CustomCallingGroups))
            {
                json["allowedcallinggroups"] = AllowedCallingGroup.Select(p =>
                {
                    var obj = new JObject();
                    if (p.Key.Data?.Length == 20)
                    {
                        obj["contract"] = p.Key.ToHashString();
                    }
                    else
                    {
                        obj["group"] = p.Key.ToString();
                    }
                    obj["trusts"] = p.Value.Select(v => (JObject)v.ToHashString()).ToArray();
                    return obj;
                }).ToArray();
            }
            return json;
        }
    }
}
