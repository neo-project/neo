using System;
using System.IO;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public abstract class TransactionAttribute : ISerializable
    {
        public abstract TransactionAttributeType Type { get; }
        public abstract bool AllowMultiple { get; }
        public virtual int Size => sizeof(TransactionAttributeType);

        protected abstract void SerializeWithoutType(BinaryWriter writer);
        protected abstract void DeserializeWithoutType(BinaryReader reader);
        protected abstract void DeserializeJson(JObject json);

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            SerializeWithoutType(writer);
        }

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != (byte)Type)
                throw new FormatException();
            DeserializeWithoutType(reader);
        }

        public static TransactionAttribute DeserializeFrom(BinaryReader reader)
        {
            TransactionAttributeType type = (TransactionAttributeType)reader.ReadByte();
            TransactionAttribute attribute = type switch
            {
                TransactionAttributeType.HighPriority => new HighPriorityAttribute(),
                TransactionAttributeType.OracleResponse => new OracleResponse(),
                _ => throw new FormatException()
            };
            attribute.DeserializeWithoutType(reader);
            return attribute;
        }

        public virtual JObject ToJson()
        {
            return new JObject
            {
                ["type"] = Type
            };
        }

        public static TransactionAttribute FromJson(JObject json)
        {
            throw new NotImplementedException();
        }
    }
}
