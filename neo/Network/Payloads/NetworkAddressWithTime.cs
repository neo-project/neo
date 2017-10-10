using Neo.IO;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace Neo.Network.Payloads
{
    internal class NetworkAddressWithTime : ISerializable
    {
        public const ulong NODE_NETWORK = 1;

        public uint Timestamp;
        public ulong Services;
        public IPEndPoint EndPoint;

        public int Size => sizeof(uint) + sizeof(ulong) + 16 + sizeof(ushort);

        public static NetworkAddressWithTime Create(IPEndPoint endpoint, ulong services, uint timestamp)
        {
            return new NetworkAddressWithTime
            {
                Timestamp = timestamp,
                Services = services,
                EndPoint = endpoint
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Timestamp = reader.ReadUInt32();
            Services = reader.ReadUInt64();
            IPAddress address;
            try
            {
                address = new IPAddress(reader.ReadBytes(16));
            }
            catch (ArgumentException ex)
            {
                throw new FormatException(ex.Message, ex);
            }
            ushort port = reader.ReadBytes(2).Reverse().ToArray().ToUInt16(0);
            EndPoint = new IPEndPoint(address, port);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Timestamp);
            writer.Write(Services);
            writer.Write(EndPoint.Address.GetAddressBytes());
            writer.Write(BitConverter.GetBytes((ushort)EndPoint.Port).Reverse().ToArray());
        }
    }
}
