// Copyright (C) 2015-2024 The Neo Project.
//
// BooleanCondition.cs file belongs to the neo project and is free
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
using System.Runtime.CompilerServices;

namespace Neo.Network.P2P.Payloads.Conditions
{
    public class BooleanCondition : WitnessCondition, IEquatable<BooleanCondition>
    {
        /// <summary>
        /// The expression of the <see cref="BooleanCondition"/>.
        /// </summary>
        public bool Expression;

        public override int Size => base.Size + sizeof(bool);
        public override WitnessConditionType Type => WitnessConditionType.Boolean;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(BooleanCondition other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null) return false;
            return
                Type == other.Type &&
                Expression == other.Expression;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj is BooleanCondition bc && Equals(bc);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Type, Expression);
        }

        protected override void DeserializeWithoutType(ref MemoryReader reader, int maxNestDepth)
        {
            Expression = reader.ReadBoolean();
        }

        public override bool Match(ApplicationEngine engine)
        {
            return Expression;
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Expression);
        }

        private protected override void ParseJson(JObject json, int maxNestDepth)
        {
            Expression = json["expression"].GetBoolean();
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["expression"] = Expression;
            return json;
        }

        public override StackItem ToStackItem(IReferenceCounter referenceCounter = null)
        {
            var result = (VM.Types.Array)base.ToStackItem(referenceCounter);
            result.Add(Expression);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(BooleanCondition left, BooleanCondition right)
        {
            if (left is null || right is null)
                return Equals(left, right);

            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(BooleanCondition left, BooleanCondition right)
        {
            if (left is null || right is null)
                return !Equals(left, right);

            return !left.Equals(right);
        }
    }
}
