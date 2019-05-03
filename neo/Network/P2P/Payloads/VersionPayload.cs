using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class VersionPayload : ISerializable
    {
        public uint Magic;
        public uint Version;
        public VersionServices Services;
        public uint Timestamp;
        public ushort Port;
        public uint Nonce;
        public string UserAgent;
        public uint StartHeight;

        public int Size =>
            sizeof(uint) +              //Magic
            sizeof(uint) +              //Version
            sizeof(VersionServices) +   //Services
            sizeof(uint) +              //Timestamp
            sizeof(ushort) +            //Port
            sizeof(uint) +              //Nonce
            UserAgent.GetVarSize() +    //UserAgent
            sizeof(uint);               //StartHeight

        public static VersionPayload Create(int port, uint nonce, string userAgent, uint startHeight)
        {
            return new VersionPayload
            {
                Magic = ProtocolSettings.Default.Magic,
                Version = LocalNode.ProtocolVersion,
                Services = VersionServices.FullNode,
                Timestamp = DateTime.Now.ToTimestamp(),
                Port = (ushort)port,
                Nonce = nonce,
                UserAgent = userAgent,
                StartHeight = startHeight,
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Magic = reader.ReadUInt32();
            Version = reader.ReadUInt32();
            Services = (VersionServices)reader.ReadUInt64();
            Timestamp = reader.ReadUInt32();
            Port = reader.ReadUInt16();
            Nonce = reader.ReadUInt32();
            UserAgent = reader.ReadVarString(1024);
            StartHeight = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write((ulong)Services);
            writer.Write(Timestamp);
            writer.Write(Port);
            writer.Write(Nonce);
            writer.WriteVarString(UserAgent);
            writer.Write(StartHeight);
        }
    }
}
