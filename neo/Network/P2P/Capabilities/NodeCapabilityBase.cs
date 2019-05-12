using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public abstract class NodeCapabilityBase : ISerializable
    {
        public virtual int Size => 1; // Type

        /// <summary>
        /// Type
        /// </summary>
        public NodeCapabilities Type { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type</param>
        protected NodeCapabilityBase(NodeCapabilities type)
        {
            Type = type;
        }

        public virtual void Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != (byte)Type)
            {
                throw new FormatException();
            }
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
        }

        /// <summary>
        /// Create a new capability
        /// </summary>
        /// <param name="type">Type</param>
        public static NodeCapabilityBase Create(NodeCapabilities type)
        {
            switch (type)
            {
                case NodeCapabilities.TcpServer:
                case NodeCapabilities.UdpServer:
                case NodeCapabilities.WsServer: return new ServerCapability(type); 
                case NodeCapabilities.FullNode: return new FullNodeCapability(); 
                case NodeCapabilities.AcceptRelay: return new AcceptRelayCapability(); 

                default: throw new FormatException();
            }
        }
    }
}