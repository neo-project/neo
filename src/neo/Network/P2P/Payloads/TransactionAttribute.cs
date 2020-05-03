using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public abstract class TransactionAttribute : ISerializable
    {
        public abstract TransactionAttributeType Type { get; }
        public virtual int Size => sizeof(TransactionAttributeType);

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != (byte)Type)
                throw new FormatException();
            DeserializeWithoutType(reader);
        }

        protected abstract void DeserializeWithoutType(BinaryReader reader);

        public virtual JObject ToJson()
        {
            return new JObject
            {
                ["type"] = Type
            };
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            SerializeWithoutType(writer);
        }

        protected abstract void SerializeWithoutType(BinaryWriter writer);
    }
}
