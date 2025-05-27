// Copyright (C) 2015-2025 The Neo Project.
//
// Signer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads.Conditions;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Array = Neo.VM.Types.Array;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents a signer of a <see cref="Transaction"/>.
    /// </summary>
    public class Signer : IInteroperable, ISerializable, IEquatable<Signer>
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
        /// The contracts that allowed by the witness.
        /// Only available when the <see cref="WitnessScope.CustomContracts"/> flag is set.
        /// </summary>
        public UInt160[] AllowedContracts;

        /// <summary>
        /// The groups that allowed by the witness.
        /// Only available when the <see cref="WitnessScope.CustomGroups"/> flag is set.
        /// </summary>
        public ECPoint[] AllowedGroups;

        /// <summary>
        /// The rules that the witness must meet.
        /// Only available when the <see cref="WitnessScope.WitnessRules"/> flag is set.
        /// </summary>
        public WitnessRule[] Rules;

        public int Size =>
            /*Account*/             UInt160.Length +
            /*Scopes*/              sizeof(WitnessScope) +
            /*AllowedContracts*/    (Scopes.HasFlag(WitnessScope.CustomContracts) ? AllowedContracts.GetVarSize() : 0) +
            /*AllowedGroups*/       (Scopes.HasFlag(WitnessScope.CustomGroups) ? AllowedGroups.GetVarSize() : 0) +
            /*Rules*/               (Scopes.HasFlag(WitnessScope.WitnessRules) ? Rules.GetVarSize() : 0);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Signer other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (other is null) return false;
            if (Account != other.Account || Scopes != other.Scopes)
                return false;

            if (Scopes.HasFlag(WitnessScope.CustomContracts) && !AllowedContracts.SequenceEqual(other.AllowedContracts))
                return false;

            if (Scopes.HasFlag(WitnessScope.CustomGroups) && !AllowedGroups.SequenceEqual(other.AllowedGroups))
                return false;

            if (Scopes.HasFlag(WitnessScope.WitnessRules) && !Rules.SequenceEqual(other.Rules))
                return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return obj is Signer signerObj && Equals(signerObj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Account.GetHashCode(), Scopes);
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Account = reader.ReadSerializable<UInt160>();
            Scopes = (WitnessScope)reader.ReadByte();
            if ((Scopes & ~(WitnessScope.CalledByEntry | WitnessScope.CustomContracts | WitnessScope.CustomGroups | WitnessScope.WitnessRules | WitnessScope.Global)) != 0)
                throw new FormatException();
            if (Scopes.HasFlag(WitnessScope.Global) && Scopes != WitnessScope.Global)
                throw new FormatException();
            AllowedContracts = Scopes.HasFlag(WitnessScope.CustomContracts)
                ? reader.ReadSerializableArray<UInt160>(MaxSubitems) : [];
            AllowedGroups = Scopes.HasFlag(WitnessScope.CustomGroups)
                ? reader.ReadSerializableArray<ECPoint>(MaxSubitems) : [];
            Rules = Scopes.HasFlag(WitnessScope.WitnessRules)
                ? reader.ReadSerializableArray<WitnessRule>(MaxSubitems) : [];
        }

        /// <summary>
        /// Converts all rules contained in the <see cref="Signer"/> object to <see cref="WitnessRule"/>.
        /// </summary>
        /// <returns>The <see cref="WitnessRule"/> array used to represent the current signer.</returns>
        public IEnumerable<WitnessRule> GetAllRules()
        {
            if (Scopes == WitnessScope.Global)
            {
                yield return new WitnessRule
                {
                    Action = WitnessRuleAction.Allow,
                    Condition = new BooleanCondition { Expression = true }
                };
            }
            else
            {
                if (Scopes.HasFlag(WitnessScope.CalledByEntry))
                {
                    yield return new WitnessRule
                    {
                        Action = WitnessRuleAction.Allow,
                        Condition = new CalledByEntryCondition()
                    };
                }
                if (Scopes.HasFlag(WitnessScope.CustomContracts))
                {
                    foreach (var hash in AllowedContracts)
                        yield return new WitnessRule
                        {
                            Action = WitnessRuleAction.Allow,
                            Condition = new ScriptHashCondition { Hash = hash }
                        };
                }
                if (Scopes.HasFlag(WitnessScope.CustomGroups))
                {
                    foreach (var group in AllowedGroups)
                        yield return new WitnessRule
                        {
                            Action = WitnessRuleAction.Allow,
                            Condition = new GroupCondition { Group = group }
                        };
                }
                if (Scopes.HasFlag(WitnessScope.WitnessRules))
                {
                    foreach (var rule in Rules)
                        yield return rule;
                }
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Account);
            writer.Write((byte)Scopes);
            if (Scopes.HasFlag(WitnessScope.CustomContracts))
                writer.Write(AllowedContracts);
            if (Scopes.HasFlag(WitnessScope.CustomGroups))
                writer.Write(AllowedGroups);
            if (Scopes.HasFlag(WitnessScope.WitnessRules))
                writer.Write(Rules);
        }

        /// <summary>
        /// Converts the signer from a JSON object.
        /// </summary>
        /// <param name="json">The signer represented by a JSON object.</param>
        /// <returns>The converted signer.</returns>
        public static Signer FromJson(JObject json)
        {
            Signer signer = new()
            {
                Account = UInt160.Parse(json["account"].GetString()),
                Scopes = Enum.Parse<WitnessScope>(json["scopes"].GetString())
            };
            if (signer.Scopes.HasFlag(WitnessScope.CustomContracts))
                signer.AllowedContracts = ((JArray)json["allowedcontracts"]).Select(p => UInt160.Parse(p.GetString())).ToArray();
            if (signer.Scopes.HasFlag(WitnessScope.CustomGroups))
                signer.AllowedGroups = ((JArray)json["allowedgroups"]).Select(p => ECPoint.Parse(p.GetString(), ECCurve.Secp256r1)).ToArray();
            if (signer.Scopes.HasFlag(WitnessScope.WitnessRules))
                signer.Rules = ((JArray)json["rules"]).Select(p => WitnessRule.FromJson((JObject)p)).ToArray();
            return signer;
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
                json["allowedcontracts"] = AllowedContracts.Select(p => (JToken)p.ToString()).ToArray();
            if (Scopes.HasFlag(WitnessScope.CustomGroups))
                json["allowedgroups"] = AllowedGroups.Select(p => (JToken)p.ToString()).ToArray();
            if (Scopes.HasFlag(WitnessScope.WitnessRules))
                json["rules"] = Rules.Select(p => p.ToJson()).ToArray();
            return json;
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            throw new NotSupportedException();
        }

        StackItem IInteroperable.ToStackItem(IReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter,
            [
                Account.ToArray(),
                (byte)Scopes,
                Scopes.HasFlag(WitnessScope.CustomContracts) ? new Array(referenceCounter, AllowedContracts.Select(u => new ByteString(u.ToArray()))) : new Array(referenceCounter),
                Scopes.HasFlag(WitnessScope.CustomGroups) ? new Array(referenceCounter, AllowedGroups.Select(u => new ByteString(u.ToArray()))) : new Array(referenceCounter),
                Scopes.HasFlag(WitnessScope.WitnessRules) ? new Array(referenceCounter, Rules.Select(u => u.ToStackItem(referenceCounter))) : new Array(referenceCounter)
            ]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Signer left, Signer right)
        {
            if (left is null || right is null)
                return Equals(left, right);

            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Signer left, Signer right)
        {
            if (left is null || right is null)
                return !Equals(left, right);

            return !left.Equals(right);
        }
    }
}
