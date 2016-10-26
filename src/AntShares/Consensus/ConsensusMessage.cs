using AntShares.IO;
using System;
using System.IO;
using System.Reflection;

namespace AntShares.Consensus
{
    internal abstract class ConsensusMessage : ISerializable
    {
        public readonly ConsensusMessageType Type;
        public byte ViewNumber;

        public int Size => sizeof(ConsensusMessageType) + sizeof(byte);

        protected ConsensusMessage(ConsensusMessageType type)
        {
            this.Type = type;
        }

        public virtual void Deserialize(BinaryReader reader)
        {
            if (Type != (ConsensusMessageType)reader.ReadByte())
                throw new FormatException();
            ViewNumber = reader.ReadByte();
        }

        public static ConsensusMessage DeserializeFrom(byte[] data)
        {
            ConsensusMessageType type = (ConsensusMessageType)data[0];
            string typeName = $"{typeof(ConsensusMessage).Namespace}.{type}";
            ConsensusMessage message = typeof(ConsensusMessage).GetTypeInfo().Assembly.CreateInstance(typeName) as ConsensusMessage;
            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader r = new BinaryReader(ms))
            {
                message.Deserialize(r);
            }
            return message;
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write(ViewNumber);
        }
    }
}
