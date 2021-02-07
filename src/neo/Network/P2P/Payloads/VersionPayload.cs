using Neo.IO;
using Neo.Network.P2P.Capabilities;
using System;
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
        public uint Nonce;
        public string UserAgent;
        public NodeCapability[] Capabilities;

        public int Size =>
            sizeof(uint) +              // Magic
            sizeof(uint) +              // Version
            sizeof(uint) +              // Timestamp
            sizeof(uint) +              // Nonce
            UserAgent.GetVarSize() +    // UserAgent
            Capabilities.GetVarSize();  // Capabilities

        public static VersionPayload Create(uint magic, uint nonce, string userAgent, params NodeCapability[] capabilities)
        {
            return new VersionPayload
            {
                Magic = magic,
                Version = LocalNode.ProtocolVersion,
                Timestamp = DateTime.Now.ToTimestamp(),
                Nonce = nonce,
                UserAgent = userAgent,
                Capabilities = capabilities,
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Magic = reader.ReadUInt32();
            Version = reader.ReadUInt32();
            Timestamp = reader.ReadUInt32();
            Nonce = reader.ReadUInt32();
            UserAgent = reader.ReadVarString(1024);

            // Capabilities
            Capabilities = new NodeCapability[reader.ReadVarInt(MaxCapabilities)];
            for (int x = 0, max = Capabilities.Length; x < max; x++)
                Capabilities[x] = NodeCapability.DeserializeFrom(reader);
            if (Capabilities.Select(p => p.Type).Distinct().Count() != Capabilities.Length)
                throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write(Timestamp);
            writer.Write(Nonce);
            writer.WriteVarString(UserAgent);
            writer.Write(Capabilities);
        }
    }
}
