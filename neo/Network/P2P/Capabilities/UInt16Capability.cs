using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public class UInt16Capability : INodeCapability
    {
        public ushort Value { get; set; }

        public int Size => sizeof(ushort);

        /// <summary>
        /// Constructor
        /// </summary>
        public UInt16Capability() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">Value</param>
        public UInt16Capability(ushort value)
        {
            Value = value;
        }

        public void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadUInt16();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Value);
        }
    }
}