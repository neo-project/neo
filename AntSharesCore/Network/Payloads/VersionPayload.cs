using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Network.Payloads
{
    public class VersionPayload : ISerializable
    {
        public uint Version;
        public ulong Services;
        public uint Timestamp;
        public ushort Port;
        public string UserAgent;
        public uint StartHeight;

        public static VersionPayload Create(int port, string userAgent, uint start_height)
        {
            return new VersionPayload
            {
                Version = LocalNode.PROTOCOL_VERSION,
                Services = NetworkAddressWithTime.NODE_NETWORK,
                Timestamp = DateTime.Now.ToTimestamp(),
                Port = (ushort)port,
                UserAgent = userAgent,
                StartHeight = start_height
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            Services = reader.ReadUInt64();
            Timestamp = reader.ReadUInt32();
            Port = reader.ReadUInt16();
            UserAgent = reader.ReadVarString();
            StartHeight = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Services);
            writer.Write(Timestamp);
            writer.Write(Port);
            writer.WriteVarString(UserAgent);
            writer.Write(StartHeight);
        }
    }
}
