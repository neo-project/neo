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
using System.IO;

namespace Neo.Network.P2P.Payloads.Conditions
{
    public class BooleanCondition : WitnessCondition
    {
        /// <summary>
        /// The expression of the <see cref="BooleanCondition"/>.
        /// </summary>
        public bool Expression;

        public override int Size => base.Size + sizeof(bool);
        public override WitnessConditionType Type => WitnessConditionType.Boolean;

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

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            var result = (VM.Types.Array)base.ToStackItem(referenceCounter);
            result.Add(Expression);
            return result;
        }
    }
}
