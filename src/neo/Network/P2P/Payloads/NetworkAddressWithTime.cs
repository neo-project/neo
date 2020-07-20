using Neo.IO;
using Neo.Network.P2P.Capabilities;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace Neo.Network.P2P.Payloads
{
    public class NetworkAddressWithTime : ISerializable
    {
        public uint Timestamp;
        public IPAddress Address;
        public NodeCapability[] Capabilities;

        public IPEndPoint EndPoint => new IPEndPoint(Address, Capabilities.Where(p => p.Type == NodeCapabilityType.TcpServer).Select(p => (ServerCapability)p).FirstOrDefault()?.Port ?? 0);
        public int Size => sizeof(uint) + 16 + Capabilities.GetVarSize();

        public static NetworkAddressWithTime Create(IPAddress address, uint timestamp, params NodeCapability[] capabilities)
        {
            return new NetworkAddressWithTime
            {
                Timestamp = timestamp,
                Address = address,
                Capabilities = capabilities
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Timestamp = reader.ReadUInt32();

            // Address
            byte[] data = reader.ReadFixedBytes(16);
            Address = new IPAddress(data).Unmap();

            // Capabilities
            Capabilities = new NodeCapability[reader.ReadVarInt(VersionPayload.MaxCapabilities)];
            for (int x = 0, max = Capabilities.Length; x < max; x++)
                Capabilities[x] = NodeCapability.DeserializeFrom(reader);
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
