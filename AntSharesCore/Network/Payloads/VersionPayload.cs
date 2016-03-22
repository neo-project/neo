using AntShares.Core;
using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Network.Payloads
{
    internal class VersionPayload : ISerializable
    {
        public uint Version;
        public ulong Services;
        public uint Timestamp;
        public ushort Port;
        public uint Nonce;
        public string UserAgent;
        public uint StartHeight;

        public static VersionPayload Create(int port, uint nonce, string userAgent)
        {
            return new VersionPayload
            {
                Version = LocalNode.PROTOCOL_VERSION,
                Services = NetworkAddressWithTime.NODE_NETWORK,
                Timestamp = DateTime.Now.ToTimestamp(),
                Port = (ushort)port,
                Nonce = nonce,
                UserAgent = userAgent,
                StartHeight = Blockchain.Default?.Height ?? 0
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            Services = reader.ReadUInt64();
            Timestamp = reader.ReadUInt32();
            Port = reader.ReadUInt16();
            Nonce = reader.ReadUInt32();
            UserAgent = reader.ReadVarString();
            StartHeight = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Services);
            writer.Write(Timestamp);
            writer.Write(Port);
            writer.Write(Nonce);
            writer.WriteVarString(UserAgent);
            writer.Write(StartHeight);
        }
    }
}
