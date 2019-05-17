using System;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public class ServerCapability : NodeCapabilityBase
    {
        public ushort Port;

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

        protected override void DeserializeWithoutType(BinaryReader reader)
        {
            Port = reader.ReadUInt16();
        }

        protected override void SerializeWithoutType(BinaryWriter writer)
        {
            writer.Write(Port);
        }
    }
}
