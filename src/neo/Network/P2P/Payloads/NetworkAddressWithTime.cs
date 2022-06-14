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
using Neo.Network.P2P.Capabilities;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Sent with an <see cref="AddrPayload"/> to respond to <see cref="MessageCommand.GetAddr"/> messages.
    /// </summary>
    public class NetworkAddressWithTime : ISerializable
    {
        /// <summary>
        /// The time when connected to the node.
        /// </summary>
        public uint Timestamp;

        /// <summary>
        /// The address of the node.
        /// </summary>
        public IPAddress Address;

        /// <summary>
        /// The capabilities of the node.
        /// </summary>
        public NodeCapability[] Capabilities;

        /// <summary>
        /// The <see cref="IPEndPoint"/> of the Tcp server.
        /// </summary>
        public IPEndPoint EndPoint => new(Address, Capabilities.Where(p => p.Type == NodeCapabilityType.TcpServer).Select(p => (ServerCapability)p).FirstOrDefault()?.Port ?? 0);

        public int Size => sizeof(uint) + 16 + Capabilities.GetVarSize();

        /// <summary>
        /// Creates a new instance of the <see cref="NetworkAddressWithTime"/> class.
        /// </summary>
        /// <param name="address">The address of the node.</param>
        /// <param name="timestamp">The time when connected to the node.</param>
        /// <param name="capabilities">The capabilities of the node.</param>
        /// <returns>The created payload.</returns>
        public static NetworkAddressWithTime Create(IPAddress address, uint timestamp, params NodeCapability[] capabilities)
        {
            return new NetworkAddressWithTime
            {
                Timestamp = timestamp,
                Address = address,
                Capabilities = capabilities
            };
        }

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            Timestamp = reader.ReadUInt32();

            // Address
            ReadOnlyMemory<byte> data = reader.ReadMemory(16);
            Address = new IPAddress(data.Span).Unmap();

            // Capabilities
            Capabilities = new NodeCapability[reader.ReadVarInt(VersionPayload.MaxCapabilities)];
            for (int x = 0, max = Capabilities.Length; x < max; x++)
                Capabilities[x] = NodeCapability.DeserializeFrom(ref reader);
            if (Capabilities.Select(p => p.Type).Distinct().Count() != Capabilities.Length)
                throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Timestamp);
            writer.Write(Address.MapToIPv6().GetAddressBytes());
            writer.Write(Capabilities);
        }
    }
}
