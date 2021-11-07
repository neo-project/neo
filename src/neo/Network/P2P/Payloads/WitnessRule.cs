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
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// The rule used to describe the scope of the witness.
    /// </summary>
    public class WitnessRule : ISerializable
    {
        /// <summary>
        /// Indicates the action to be taken if the current context meets with the rule.
        /// </summary>
        public WitnessRuleAction Action;

        /// <summary>
        /// The condition of the rule.
        /// </summary>
        public WitnessCondition Condition;

        int ISerializable.Size => sizeof(WitnessRuleAction) + Condition.Size;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Action = (WitnessRuleAction)reader.ReadByte();
            if (Action != WitnessRuleAction.Allow && Action != WitnessRuleAction.Deny)
                throw new FormatException();
            Condition = WitnessCondition.DeserializeFrom(reader);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Action);
            writer.Write(Condition);
        }

        /// <summary>
        /// Converts the rule to a JSON object.
        /// </summary>
        /// <returns>The rule represented by a JSON object.</returns>
        public JObject ToJson()
        {
            return new JObject
            {
                ["action"] = Action,
                ["condition"] = Condition.ToJson()
            };
        }
    }
}
