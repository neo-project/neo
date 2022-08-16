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
using Neo.Persistence;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents an attribute of a transaction.
    /// </summary>
    public abstract class TransactionAttribute : ISerializable
    {
        /// <summary>
        /// The type of the attribute.
        /// </summary>
        public abstract TransactionAttributeType Type { get; }

        /// <summary>
        /// Indicates whether multiple instances of this attribute are allowed.
        /// </summary>
        public abstract bool AllowMultiple { get; }

        public virtual int Size => sizeof(TransactionAttributeType);

        public void Deserialize(ref MemoryReader reader)
        {
            if (reader.ReadByte() != (byte)Type)
                throw new FormatException();
            DeserializeWithoutType(ref reader);
        }

        /// <summary>
        /// Deserializes an <see cref="TransactionAttribute"/> object from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <returns>The deserialized attribute.</returns>
        public static TransactionAttribute DeserializeFrom(ref MemoryReader reader)
        {
            TransactionAttributeType type = (TransactionAttributeType)reader.ReadByte();
            if (ReflectionCache<TransactionAttributeType>.CreateInstance(type) is not TransactionAttribute attribute)
                throw new FormatException();
            attribute.DeserializeWithoutType(ref reader);
            return attribute;
        }

        /// <summary>
        /// Deserializes the <see cref="TransactionAttribute"/> object from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        protected abstract void DeserializeWithoutType(ref MemoryReader reader);

        /// <summary>
        /// Converts the attribute to a JSON object.
        /// </summary>
        /// <returns>The attribute represented by a JSON object.</returns>
        public virtual JObject ToJson()
        {
            return new JObject
            {
                ["type"] = Type
            };
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            SerializeWithoutType(writer);
        }

        /// <summary>
        /// Serializes the <see cref="TransactionAttribute"/> object to a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        protected abstract void SerializeWithoutType(BinaryWriter writer);

        /// <summary>
        /// Verifies the attribute with the transaction.
        /// </summary>
        /// <param name="snapshot">The snapshot used to verify the attribute.</param>
        /// <param name="tx">The <see cref="Transaction"/> that contains the attribute.</param>
        /// <returns><see langword="true"/> if the verification passes; otherwise, <see langword="false"/>.</returns>
        public virtual bool Verify(DataCache snapshot, Transaction tx) => true;
    }
}
