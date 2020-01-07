using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public abstract class NodeCapability : ISerializable
    {
        /// <summary>
        /// Type
        /// </summary>
        public readonly NodeCapabilityType Type;

        public virtual int Size => sizeof(NodeCapabilityType); // Type

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type</param>
        protected NodeCapability(NodeCapabilityType type)
        {
            this.Type = type;
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != (byte)Type)
            {
                throw new FormatException();
            }

            DeserializeWithoutType(reader);
        }

        public static NodeCapability DeserializeFrom(BinaryReader reader)
        {
            NodeCapability capability;
            NodeCapabilityType type = (NodeCapabilityType)reader.ReadByte();
            switch (type)
            {
                case NodeCapabilityType.TcpServer:
                case NodeCapabilityType.WsServer:
                    capability = new ServerCapability(type);
                    break;
                case NodeCapabilityType.FullNode:
                    capability = new FullNodeCapability();
                    break;
                default:
                    throw new FormatException();
            }
            capability.DeserializeWithoutType(reader);
            return capability;
        }

        protected abstract void DeserializeWithoutType(BinaryReader reader);

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            SerializeWithoutType(writer);
        }

        protected abstract void SerializeWithoutType(BinaryWriter writer);
    }
}
