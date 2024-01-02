// Copyright (C) 2015-2024 The Neo Project.
//
// NodeCapability.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    /// <summary>
    /// Represents the capabilities of a NEO node.
    /// </summary>
    public abstract class NodeCapability : ISerializable
    {
        /// <summary>
        /// Indicates the type of the <see cref="NodeCapability"/>.
        /// </summary>
        public readonly NodeCapabilityType Type;

        public virtual int Size => sizeof(NodeCapabilityType); // Type

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeCapability"/> class.
        /// </summary>
        /// <param name="type">The type of the <see cref="NodeCapability"/>.</param>
        protected NodeCapability(NodeCapabilityType type)
        {
            this.Type = type;
        }

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            if (reader.ReadByte() != (byte)Type)
            {
                throw new FormatException();
            }

            DeserializeWithoutType(ref reader);
        }

        /// <summary>
        /// Deserializes an <see cref="NodeCapability"/> object from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        /// <returns>The deserialized <see cref="NodeCapability"/>.</returns>
        public static NodeCapability DeserializeFrom(ref MemoryReader reader)
        {
            NodeCapabilityType type = (NodeCapabilityType)reader.ReadByte();
            NodeCapability capability = type switch
            {
#pragma warning disable CS0612 // Type or member is obsolete
                NodeCapabilityType.TcpServer or NodeCapabilityType.WsServer => new ServerCapability(type),
#pragma warning restore CS0612 // Type or member is obsolete
                NodeCapabilityType.FullNode => new FullNodeCapability(),
                _ => throw new FormatException(),
            };
            capability.DeserializeWithoutType(ref reader);
            return capability;
        }

        /// <summary>
        /// Deserializes the <see cref="NodeCapability"/> object from a <see cref="MemoryReader"/>.
        /// </summary>
        /// <param name="reader">The <see cref="MemoryReader"/> for reading data.</param>
        protected abstract void DeserializeWithoutType(ref MemoryReader reader);

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            SerializeWithoutType(writer);
        }

        /// <summary>
        /// Serializes the <see cref="NodeCapability"/> object to a <see cref="BinaryWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter"/> for writing data.</param>
        protected abstract void SerializeWithoutType(BinaryWriter writer);
    }
}
