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
using Neo.IO.Caching;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public abstract class WitnessCondition : ISerializable
    {
        private const int MaxSubitems = 16;

        /// <summary>
        /// The type of the <see cref="WitnessCondition"/>.
        /// </summary>
        public abstract WitnessConditionType Type { get; }

        public virtual int Size => sizeof(WitnessConditionType);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != (byte)Type) throw new FormatException();
            DeserializeWithoutType(reader);
        }

        /// <summary>
        /// Deserializes an <see cref="WitnessCondition"/> array from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        /// <returns>The deserialized <see cref="WitnessCondition"/> array.</returns>
        protected static WitnessCondition[] DeserializeConditions(BinaryReader reader)
        {
            WitnessCondition[] conditions = new WitnessCondition[reader.ReadVarInt(MaxSubitems)];
            for (int i = 0; i < conditions.Length; i++)
                conditions[i] = DeserializeFrom(reader);
            return conditions;
        }

        /// <summary>
        /// Deserializes an <see cref="WitnessCondition"/> object from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        /// <returns>The deserialized <see cref="WitnessCondition"/>.</returns>
        public static WitnessCondition DeserializeFrom(BinaryReader reader)
        {
            WitnessConditionType type = (WitnessConditionType)reader.ReadByte();
            if (ReflectionCache<WitnessConditionType>.CreateInstance(type) is not WitnessCondition condition)
                throw new FormatException();
            condition.DeserializeWithoutType(reader);
            return condition;
        }

        /// <summary>
        /// Deserializes the <see cref="WitnessCondition"/> object from a <see cref="BinaryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="BinaryReader"/> for reading data.</param>
        protected abstract void DeserializeWithoutType(BinaryReader reader);

        /// <summary>
        /// Checks whether the current context matches the condition.
        /// </summary>
        /// <param name="engine">The <see cref="ApplicationEngine"/> that is executing CheckWitness.</param>
        /// <returns><see langword="true"/> if the condition matches; otherwise, <see langword="false"/>.</returns>
        public abstract bool Match(ApplicationEngine engine);

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            SerializeWithoutType(writer);
        }

        /// <summary>
        /// Serializes the <see cref="WitnessCondition"/> object to a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        protected abstract void SerializeWithoutType(BinaryWriter writer);

        /// <summary>
        /// Converts the condition to a JSON object.
        /// </summary>
        /// <returns>The condition represented by a JSON object.</returns>
        public virtual JObject ToJson()
        {
            return new JObject
            {
                ["type"] = Type
            };
        }
    }
}
