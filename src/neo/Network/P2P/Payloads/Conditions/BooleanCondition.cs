// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Json;
using Neo.SmartContract;
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

        protected override void DeserializeWithoutType(BinaryReader reader)
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

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["expression"] = Expression;
            return json;
        }
    }
}
