// Copyright (C) 2015-2024 The Neo Project.
//
// ServerCapability.cs file belongs to the neo project and is free
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
    /// Indicates that the node is a server.
    /// </summary>
    public class ServerCapability : NodeCapability
    {
        /// <summary>
        /// Indicates the port that the node is listening on.
        /// </summary>
        public ushort Port;

        public override int Size =>
            base.Size +     // Type
            sizeof(ushort); // Port

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerCapability"/> class.
        /// </summary>
        /// <param name="type">The type of the <see cref="ServerCapability"/>. It must be <see cref="NodeCapabilityType.TcpServer"/> or <see cref="NodeCapabilityType.WsServer"/></param>
        /// <param name="port">The port that the node is listening on.</param>
        public ServerCapability(NodeCapabilityType type, ushort port = 0) : base(type)
        {
#pragma warning disable CS0612 // Type or member is obsolete
            if (type != NodeCapabilityType.TcpServer && type != NodeCapabilityType.WsServer)
#pragma warning restore CS0612 // Type or member is obsolete
            {
                throw new ArgumentException(nameof(type));
            }

            Port = port;
        }

        protected override void DeserializeWithoutType(ref MemoryReader reader)
        {
            Port = reader.ReadUInt16();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Port);
        }
    }
}
