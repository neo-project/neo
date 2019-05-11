using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public class ServerCapability : NodeCapabilityBase
    {
        public enum ChannelType : byte
        {
            Tcp = 0x00,
            Udp = 0x01,
            Websocket = 0x02
        }

        public ChannelType Channel { get; set; }
        public ushort Port { get; set; }
        public override int Size =>
            base.Size +     // Type
            sizeof(byte) +  // Channel
            sizeof(ushort); //Port

        /// <summary>
        /// Constructor
        /// </summary>
        public ServerCapability() : base(NodeCapabilities.Server) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Channel</param>
        /// <param name="port">Port</param>
        public ServerCapability(ChannelType type, ushort port) : base(NodeCapabilities.Server)
        {
            Channel = type;
            Port = port;
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);

            Channel = (ChannelType)reader.ReadByte();
            Port = reader.ReadUInt16();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)Type);
            writer.Write(Port);
        }
    }
}