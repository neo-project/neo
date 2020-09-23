using System;
using System.IO;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public abstract class TransactionAttribute
    {
        public abstract bool AllowMultiple { get; }

        public static TransactionAttribute DeserializeFrom(BinaryReader reader)
        {
            var type = (TransactionAttributeType)reader.ReadByte();
            var attribute = CreateAttribute(type);
            attribute.Deserialize(type, reader);
            return attribute;
        }

        private static TransactionAttribute CreateAttribute(TransactionAttributeType type) => type switch
        {
            TransactionAttributeType.HighPriority => new HighPriorityAttribute(),
            TransactionAttributeType.OracleResponse => new OracleResponse(),
            _ => throw new FormatException(),
        };

        public abstract int Size { get; }

        public abstract void Serialize(BinaryWriter writer);
        protected abstract void Deserialize(TransactionAttributeType type, BinaryReader reader);
        protected abstract void FromJson(TransactionAttributeType type, JObject json);

        public abstract JObject ToJson();

        public static TransactionAttribute FromJson(JObject json)
        {
            var type = Enum.Parse<TransactionAttributeType>(json["type"].AsString());
            var attribute = CreateAttribute(type);
            attribute.FromJson(type, json);
            return attribute;
        }
    }
}
