using Neo.IO;
using Neo.IO.Caching;
using Neo.Network.P2P.Capabilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class VersionPayload : ISerializable
    {
        /// <summary>
        /// Reflection cache for ConsensusMessageType
        /// </summary>
        private static readonly ReflectionCache<byte> ReflectionCache = ReflectionCache<byte>.CreateFromEnum<NodeCapabilities>();

        const int MaxCapabilities = 32;

        public uint Magic;
        public uint Version;
        public VersionServices Services;
        public uint Timestamp;
        public uint Nonce;
        public string UserAgent;
        public uint StartHeight;
        public NodeCapabilityBase[] Capabilities;

        public int Size =>
            sizeof(uint) +              //Magic
            sizeof(uint) +              //Version
            sizeof(VersionServices) +   //Services
            sizeof(uint) +              //Timestamp
            sizeof(uint) +              //Nonce
            UserAgent.GetVarSize() +    //UserAgent
            sizeof(uint) +              //StartHeight
            Capabilities.GetVarSize();  //Capabilities

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
            Nonce = reader.ReadUInt32();
            UserAgent = reader.ReadVarString(1024);
            StartHeight = reader.ReadUInt32();

            // Capabilities

            Capabilities = new NodeCapabilityBase[reader.ReadVarInt(MaxCapabilities)];

            for (int x = 0, max = Capabilities.Length; x < max; x++)
            {
                var type = reader.PeekChar();

                if (!ReflectionCache.TryGetValue((byte)type, out var objType))
                {
                    throw new FormatException();
                }

                var value = (NodeCapabilityBase)Activator.CreateInstance(objType);
                value.Deserialize(reader);

                Capabilities[x] = value;
            }
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.Write(Version);
            writer.Write((ulong)Services);
            writer.Write(Timestamp);
            writer.Write(Nonce);
            writer.WriteVarString(UserAgent);
            writer.Write(StartHeight);

            // Capabilities

            writer.WriteVarInt(Capabilities.Length);
            foreach (var value in Capabilities)
            {
                value.Serialize(writer);
            }
        }
    }
}
