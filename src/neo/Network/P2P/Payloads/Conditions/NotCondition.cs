// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads.Conditions
{
    /// <summary>
    /// Reverse another condition.
    /// </summary>
    public class NotCondition : WitnessCondition
    {
        /// <summary>
        /// The expression of the condition to be reversed.
        /// </summary>
        public WitnessCondition Expression;

        public override int Size => base.Size + Expression.Size;
        public override WitnessConditionType Type => WitnessConditionType.Not;

        protected override void DeserializeWithoutType(BinaryReader reader, int maxNestDepth)
        {
            if (maxNestDepth <= 0) throw new FormatException();
            Expression = DeserializeFrom(reader, maxNestDepth - 1);
        }

        public override bool Match(ApplicationEngine engine)
        {
            return !Expression.Match(engine);
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Expression);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["expression"] = Expression.ToJson();
            return json;
        }
    }
}
