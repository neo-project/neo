// Copyright (C) 2015-2024 The Neo Project.
//
// CalledByEntryCondition.cs file belongs to the neo project and is free
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
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads.Conditions
{
    public class CalledByEntryCondition : WitnessCondition, IEquatable<CalledByEntryCondition>
    {
        public override WitnessConditionType Type => WitnessConditionType.CalledByEntry;

        public bool Equals(CalledByEntryCondition other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (other is null) return false;
            return Type == other.Type &&
                Size == other.Size;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is not CalledByEntryCondition cc)
                return false;
            else
                return Equals(cc);
        }

        public override int GetHashCode()
        {
            return (byte)Type;
        }

        public override bool Match(ApplicationEngine engine)
        {
            var state = engine.CurrentContext.GetState<ExecutionContextState>();
            if (state.CallingContext is null) return true;
            state = state.CallingContext.GetState<ExecutionContextState>();
            return state.CallingContext is null;
        }

        protected override void DeserializeWithoutType(ref MemoryReader reader, int maxNestDepth) { }

        protected override void SerializeWithoutType(BinaryWriter writer) { }

        private protected override void ParseJson(JObject json, int maxNestDepth) { }

        public static bool operator ==(CalledByEntryCondition left, CalledByEntryCondition right)
        {
            if (((object)left) == null || ((object)right) == null)
                return Equals(left, right);

            return left.Equals(right);
        }

        public static bool operator !=(CalledByEntryCondition left, CalledByEntryCondition right)
        {
            if (((object)left) == null || ((object)right) == null)
                return !Equals(left, right);

            return !(left.Equals(right));
        }
    }
}
