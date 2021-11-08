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
using System.Linq;

namespace Neo.Network.P2P.Payloads.Conditions
{
    /// <summary>
    /// Represents the condition that all conditions must be met.
    /// </summary>
    public class AndCondition : WitnessCondition
    {
        /// <summary>
        /// The expressions of the condition.
        /// </summary>
        public WitnessCondition[] Expressions;

        public override int Size => base.Size + Expressions.GetVarSize();
        public override WitnessConditionType Type => WitnessConditionType.And;

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            Expressions = DeserializeConditions(reader);
            if (Expressions.Length == 0) throw new FormatException();
        }

        public override bool Match(ApplicationEngine engine)
        {
            return Expressions.All(p => p.Match(engine));
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Expressions);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["expressions"] = Expressions.Select(p => p.ToJson()).ToArray();
            return json;
        }
    }
}
