using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Capabilities
{
    public abstract class NodeCapabilityBase : ISerializable
    {
        public virtual int Size => 1;

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
    }
}