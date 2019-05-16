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
        public const int MaxCapabilities = 32;

        public uint Magic;
        public uint Version;
        public uint Timestamp;
        public NodeCapabilityBase[] Capabilities;
        public uint Nonce;
        public string UserAgent;

        public int Size =>
            sizeof(uint) +              // Magic
            sizeof(uint) +              // Version
            sizeof(uint) +              // Timestamp
            Capabilities.GetVarSize() + // Capabilities
            sizeof(uint) +              // Nonce
            UserAgent.GetVarSize();     // UserAgent

        public static VersionPayload Create(uint nonce, string userAgent, IEnumerable<NodeCapabilityBase> capabilities)
        {
            return new VersionPayload
            {
                Magic = ProtocolSettings.Default.Magic,
                Version = LocalNode.ProtocolVersion,
                Timestamp = DateTime.Now.ToTimestamp(),
                Nonce = nonce,
                UserAgent = userAgent,
                Capabilities = capabilities.ToArray(),
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Magic = reader.ReadUInt32();
            Version = reader.ReadUInt32();
            Timestamp = reader.ReadUInt32();

            // Capabilities

            Capabilities = new NodeCapabilityBase[reader.ReadVarInt(MaxCapabilities)];

            for (int x = 0, max = Capabilities.Length; x < max; x++)
            {
                Capabilities[x] = NodeCapabilityBase.Create((NodeCapabilities)reader.ReadByte());
                Capabilities[x].Deserialize(reader);
            }

            Nonce = reader.ReadUInt32();
            UserAgent = reader.ReadVarString(1024);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(Timestamp);
            writer.Write(Capabilities);
            writer.Write(Nonce);
            writer.WriteVarString(UserAgent);
        }
    }
}
