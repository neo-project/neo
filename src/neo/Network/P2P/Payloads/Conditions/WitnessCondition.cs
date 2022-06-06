// Copyright (C) 2015-2022 The Neo Project.
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
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads.Conditions
{
    public abstract class WitnessCondition : IInteroperable, ISerializable
    {
        private const int MaxSubitems = 16;
        internal const int MaxNestingDepth = 2;

        /// <summary>
        /// The type of the <see cref="WitnessCondition"/>.
        /// </summary>
        public abstract WitnessConditionType Type { get; }

        public virtual int Size => sizeof(WitnessConditionType);

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            if (reader.ReadByte() != (byte)Type) throw new FormatException();
            DeserializeWithoutType(ref reader, MaxNestingDepth);
        }

        /// <summary>
        /// Deserializes an <see cref="WitnessCondition"/> array from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <param name="maxNestDepth">The maximum nesting depth allowed during deserialization.</param>
        /// <returns>The deserialized <see cref="WitnessCondition"/> array.</returns>
        protected static WitnessCondition[] DeserializeConditions(ref MemoryReader reader, int maxNestDepth)
        {
            WitnessCondition[] conditions = new WitnessCondition[reader.ReadVarInt(MaxSubitems)];
            for (int i = 0; i < conditions.Length; i++)
                conditions[i] = DeserializeFrom(ref reader, maxNestDepth);
            return conditions;
        }

        /// <summary>
        /// Deserializes an <see cref="WitnessCondition"/> object from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <param name="maxNestDepth">The maximum nesting depth allowed during deserialization.</param>
        /// <returns>The deserialized <see cref="WitnessCondition"/>.</returns>
        public static WitnessCondition DeserializeFrom(ref MemoryReader reader, int maxNestDepth)
        {
            WitnessConditionType type = (WitnessConditionType)reader.ReadByte();
            if (ReflectionCache<WitnessConditionType>.CreateInstance(type) is not WitnessCondition condition)
                throw new FormatException();
            condition.DeserializeWithoutType(ref reader, maxNestDepth);
            return condition;
        }

        /// <summary>
        /// Deserializes the <see cref="WitnessCondition"/> object from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <param name="maxNestDepth">The maximum nesting depth allowed during deserialization.</param>
        protected abstract void DeserializeWithoutType(ref MemoryReader reader, int maxNestDepth);

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

        private protected virtual void ParseJson(JObject json)
        {
        }

        /// <summary>
        /// Converts the <see cref="WitnessCondition"/> from a JSON object.
        /// </summary>
        /// <param name="json">The <see cref="WitnessCondition"/> represented by a JSON object.</param>
        /// <returns>The converted <see cref="WitnessCondition"/>.</returns>
        public static WitnessCondition FromJson(JObject json)
        {
            WitnessConditionType type = Enum.Parse<WitnessConditionType>(json["type"].GetString());
            if (ReflectionCache<WitnessConditionType>.CreateInstance(type) is not WitnessCondition condition)
                throw new FormatException("Invalid WitnessConditionType.");
            condition.ParseJson(json);
            return condition;
        }

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

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            throw new NotSupportedException();
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new VM.Types.Array(referenceCounter, new StackItem[] { (byte)Type });
        }
    }
}
