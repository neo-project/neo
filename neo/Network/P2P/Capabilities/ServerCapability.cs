using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public class ServerCapability : INodeCapability
    {
        public enum ChannelType : byte
        {
            Tcp = 0x00,
            Udp = 0x01,
            Websocket = 0x02
        }

        public ChannelType Channel { get; set; }
        public ushort Value { get; set; }
        public int Size => sizeof(ushort);

        public NodeCapabilities Type => NodeCapabilities.Server;

        /// <summary>
        /// Constructor
        /// </summary>
        public ServerCapability() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="value">Value</param>
        public ServerCapability(ChannelType type, ushort value)
        {
            Channel = type;
            Value = value;
        }

        public void Deserialize(BinaryReader reader)
        {
            Channel = (ChannelType)reader.ReadByte();
            Value = reader.ReadUInt16();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write(Value);
        }
    }
}