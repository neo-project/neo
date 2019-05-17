using Neo.IO;
using Neo.Network.P2P.Capabilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Neo.Network.P2P.Payloads
{
    public class NetworkAddressWithTime : ISerializable
    {
        public uint Timestamp;
        public IPEndPoint EndPoint;
        public NodeCapabilityBase[] Capabilities;

        public int Size => sizeof(uint) + 16 + sizeof(ushort) + Capabilities.GetVarSize();

        public static NetworkAddressWithTime Create(IPEndPoint endpoint, uint timestamp, IEnumerable<NodeCapabilityBase> capabilities)
        {
            return new NetworkAddressWithTime
            {
                Timestamp = timestamp,
                EndPoint = endpoint,
                Capabilities = capabilities.ToArray()
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Timestamp = reader.ReadUInt32();
            byte[] data = reader.ReadBytes(16);
            if (data.Length != 16) throw new FormatException();
            IPAddress address = new IPAddress(data).Unmap();
            data = reader.ReadBytes(2);
            if (data.Length != 2) throw new FormatException();
            ushort port = data.Reverse().ToArray().ToUInt16(0);
            EndPoint = new IPEndPoint(address, port);

            // Capabilities

            Capabilities = new NodeCapabilityBase[reader.ReadVarInt(VersionPayload.MaxCapabilities)];

            for (int x = 0, max = Capabilities.Length; x < max; x++)
                Capabilities[x] = NodeCapabilityBase.DeserializeFrom(reader);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Timestamp);
            writer.Write(EndPoint.Address.MapToIPv6().GetAddressBytes());
            writer.Write(BitConverter.GetBytes((ushort)EndPoint.Port).Reverse().ToArray());
            writer.Write(Capabilities);
        }
    }
}
