using AntShares.IO;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace AntShares.Network.Payloads
{
    internal class NetworkAddressWithTime : ISerializable
    {
        public UInt32 Timestamp;
        public UInt64 Services;
        public byte[] Address;
        public UInt16 Port;

        public static NetworkAddressWithTime Create(IPEndPoint endpoint, UInt64 services, UInt32 timestamp)
        {
            return new NetworkAddressWithTime
            {
                Timestamp = timestamp,
                Services = services,
                Address = endpoint.Address.MapToIPv6().GetAddressBytes(),
                Port = (UInt16)endpoint.Port
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            this.Timestamp = reader.ReadUInt32();
            this.Services = reader.ReadUInt64();
            this.Address = reader.ReadBytes(16);
            this.Port = reader.ReadUInt16();
        }

        public IPAddress GetIPAddress()
        {
            IPAddress ip = new IPAddress(Address);
            if (ip.IsIPv4MappedToIPv6)
                ip = new IPAddress(Address.Skip(12).ToArray());
            return ip;
        }

        public IPEndPoint GetIPEndPoint()
        {
            return new IPEndPoint(GetIPAddress(), Port);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Timestamp);
            writer.Write(Services);
            writer.Write(Address);
            writer.Write(Port);
        }
    }
}
