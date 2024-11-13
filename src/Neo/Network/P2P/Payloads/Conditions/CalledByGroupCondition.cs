// Copyright (C) 2015-2024 The Neo Project.
//
// CalledByGroupCondition.cs file belongs to the neo project and is free
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
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Neo.Network.P2P.Payloads.Conditions
{
    public class CalledByGroupCondition : WitnessCondition, IEquatable<CalledByGroupCondition>
    {
        /// <summary>
        /// The group to be checked.
        /// </summary>
        public ECPoint Group;

        public override int Size => base.Size + Group.Size;
        public override WitnessConditionType Type => WitnessConditionType.CalledByGroup;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(CalledByGroupCondition other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null) return false;
            return
                Type == other.Type &&
                Group == other.Group;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj is CalledByGroupCondition cc && Equals(cc);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Group);
        }

        protected override void DeserializeWithoutType(ref MemoryReader reader, int maxNestDepth)
        {
            Group = reader.ReadSerializable<ECPoint>();
        }

        public override bool Match(ApplicationEngine engine)
        {
            engine.ValidateCallFlags(CallFlags.ReadStates);
            ContractState contract = NativeContract.ContractManagement.GetContract(engine.SnapshotCache, engine.CallingScriptHash);
            return contract is not null && contract.Manifest.Groups.Any(p => p.PubKey.Equals(Group));
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Group);
        }

        private protected override void ParseJson(JObject json, int maxNestDepth)
        {
            Group = ECPoint.Parse(json["group"].GetString(), ECCurve.Secp256r1);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["group"] = Group.ToString();
            return json;
        }

        public override StackItem ToStackItem(IReferenceCounter referenceCounter)
        {
            var result = (VM.Types.Array)base.ToStackItem(referenceCounter);
            result.Add(Group.ToArray());
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(CalledByGroupCondition left, CalledByGroupCondition right)
        {
            if (left is null || right is null)
                return Equals(left, right);

            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(CalledByGroupCondition left, CalledByGroupCondition right)
        {
            if (left is null || right is null)
                return !Equals(left, right);

            return !left.Equals(right);
        }
    }
}
