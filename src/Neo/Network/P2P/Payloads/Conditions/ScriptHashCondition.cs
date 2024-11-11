// Copyright (C) 2015-2024 The Neo Project.
//
// ScriptHashCondition.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.Json;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads.Conditions
{
    public class ScriptHashCondition : WitnessCondition, IEquatable<ScriptHashCondition>
    {
        /// <summary>
        /// The script hash to be checked.
        /// </summary>
        public UInt160 Hash;

        public override int Size => base.Size + UInt160.Length;
        public override WitnessConditionType Type => WitnessConditionType.ScriptHash;

        public bool Equals(ScriptHashCondition other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null) return false;
            return Type == other.Type &&
                Size == other.Size &&
                Hash == other.Hash;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is not ScriptHashCondition sc)
                return false;
            else
                return Equals(sc);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Hash);
        }

        protected override void DeserializeWithoutType(ref MemoryReader reader, int maxNestDepth)
        {
            Hash = reader.ReadSerializable<UInt160>();
        }

        public override bool Match(ApplicationEngine engine)
        {
            return engine.CurrentScriptHash == Hash;
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Hash);
        }

        private protected override void ParseJson(JObject json, int maxNestDepth)
        {
            Hash = UInt160.Parse(json["hash"].GetString());
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["hash"] = Hash.ToString();
            return json;
        }

        public override StackItem ToStackItem(IReferenceCounter referenceCounter)
        {
            var result = (VM.Types.Array)base.ToStackItem(referenceCounter);
            result.Add(Hash.ToArray());
            return result;
        }

        public static bool operator ==(ScriptHashCondition left, ScriptHashCondition right)
        {
            if (((object)left) == null || ((object)right) == null)
                return Equals(left, right);

            return left.Equals(right);
        }

        public static bool operator !=(ScriptHashCondition left, ScriptHashCondition right)
        {
            if (((object)left) == null || ((object)right) == null)
                return !Equals(left, right);

            return !(left.Equals(right));
        }
    }
}
