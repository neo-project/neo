using Neo.IO;
using Neo.Network.P2P.Capabilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class VersionPayload : ISerializable
    {
        const int MaxCapabilities = 32;

        public uint Magic;
        public uint Version;
        public VersionServices Services;
        public uint Timestamp;
        public NodeCapabilityBase[] Capabilities;
        public uint Nonce;
        public string UserAgent;
        public uint StartHeight;

        public int Size =>
            sizeof(uint) +              // Magic
            sizeof(uint) +              // Version
            sizeof(VersionServices) +   // Services
            sizeof(uint) +              // Timestamp
            Capabilities.GetVarSize() + // Capabilities
            sizeof(uint) +              // Nonce
            UserAgent.GetVarSize() +    // UserAgent
            sizeof(uint);               // StartHeight

        public static VersionPayload Create(uint nonce, string userAgent, uint startHeight, IEnumerable<NodeCapabilityBase> capabilities)
        {
            return new VersionPayload
            {
                Magic = ProtocolSettings.Default.Magic,
                Version = LocalNode.ProtocolVersion,
                Services = VersionServices.FullNode,
                Timestamp = DateTime.Now.ToTimestamp(),
                Nonce = nonce,
                UserAgent = userAgent,
                StartHeight = startHeight,
                Capabilities = capabilities.ToArray(),
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Magic = reader.ReadUInt32();
            Version = reader.ReadUInt32();
            Services = (VersionServices)reader.ReadUInt64();
            Timestamp = reader.ReadUInt32();
            
            // Capabilities

            Capabilities = new NodeCapabilityBase[reader.ReadVarInt(MaxCapabilities)];

            for (int x = 0, max = Capabilities.Length; x < max; x++)
            {
                var cap = (NodeCapabilities)reader.PeekChar();

                switch ((NodeCapabilities)reader.PeekChar())
                {
                    case NodeCapabilities.TcpServer:
                    case NodeCapabilities.UdpServer:
                    case NodeCapabilities.WsServer: Capabilities[x] = new ServerCapability(cap); break;

                    default: throw new FormatException();
                }

                Capabilities[x].Deserialize(reader);
            }

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
            
            // Capabilities

            writer.WriteVarInt(Capabilities.Length);
            foreach (var value in Capabilities)
            {
                value.Serialize(writer);
            }

            writer.Write(Nonce);
            writer.WriteVarString(UserAgent);
            writer.Write(StartHeight);
        }
    }
}
