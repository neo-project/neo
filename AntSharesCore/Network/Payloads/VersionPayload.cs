using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Network.Payloads
{
    public class VersionPayload : ISerializable
    {
        public UInt32 Version;
        public UInt64 Services;
        public UInt32 Timestamp;
        public UInt16 Port;
        public string UserAgent;
        public UInt32 StartHeight;

        public static VersionPayload Create(int port, string userAgent, UInt32 start_height)
        {
            return new VersionPayload
            {
                Version = LocalNode.PROTOCOL_VERSION,
                Services = 1,
                Timestamp = DateTime.Now.ToTimestamp(),
                Port = (UInt16)port,
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
