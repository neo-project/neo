using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public abstract class NodeCapabilityBase : ISerializable
    {
        /// <summary>
        /// Type
        /// </summary>
        public readonly NodeCapabilities Type;

        public virtual int Size => sizeof(NodeCapabilities); // Type

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type</param>
        protected NodeCapabilityBase(NodeCapabilities type)
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

        public static NodeCapabilityBase DeserializeFrom(BinaryReader reader)
        {
            NodeCapabilityBase result;
            NodeCapabilities type = (NodeCapabilities)reader.ReadByte();
            switch (type)
            {
                case NodeCapabilities.TcpServer:
                case NodeCapabilities.UdpServer:
                case NodeCapabilities.WsServer:
                    result = new ServerCapability(type);
                    break;
                case NodeCapabilities.FullNode:
                    result = new FullNodeCapability();
                    break;
                default:
                    throw new FormatException();
            }
            result.DeserializeWithoutType(reader);
            return result;
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
