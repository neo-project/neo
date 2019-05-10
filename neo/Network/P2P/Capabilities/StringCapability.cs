using Neo.IO;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public class StringCapability : INodeCapability
    {
        const int MaxStringCapability = byte.MaxValue;

        public string Value { get; set; }

        public int Size => Value.GetVarSize();

        /// <summary>
        /// Constructor
        /// </summary>
        public StringCapability() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="value">Value</param>
        public StringCapability(string value)
        {
            Value = value
        }

        public void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadVarString(MaxStringCapability);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteVarString(Value);
        }
    }
}