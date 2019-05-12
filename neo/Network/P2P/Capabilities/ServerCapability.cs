using System;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public class ServerCapability : NodeCapabilityBase
    {
        public ushort Port { get; set; }

        public override int Size =>
            base.Size +     // Type
            sizeof(ushort); // Port

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Channel</param>
        /// <param name="port">Port</param>
        public ServerCapability(NodeCapabilities type, ushort port = 0) : base(type)
        {
            if (type != NodeCapabilities.TcpServer && type != NodeCapabilities.UdpServer && type != NodeCapabilities.WsServer)
            {
                throw new ArgumentException(nameof(type));
            }

            Port = port;
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Port = reader.ReadUInt16();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Port);
        }
    }
}