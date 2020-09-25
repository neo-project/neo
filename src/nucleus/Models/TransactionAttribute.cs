using System;
using System.IO;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public abstract class TransactionAttribute : ISerializable
    {
        public abstract bool AllowMultiple { get; }

        public static TransactionAttribute DeserializeFrom(BinaryReader reader)
        {
            var type = (TransactionAttributeType)reader.ReadByte();
            var attribute = CreateAttribute(type);
            attribute.Deserialize(type, reader);
            return attribute;
        }

        public static TransactionAttribute FromJson(JObject json)
        {
            TransactionAttributeType type = Enum.Parse<TransactionAttributeType>(json["type"].AsString());
            var attribute = CreateAttribute(type);
            attribute.DeserializeJson(type, json);
            return attribute;
        }

        private static TransactionAttribute CreateAttribute(TransactionAttributeType type) 
            => type switch
            {
                TransactionAttributeType.HighPriority => new HighPriorityAttribute(),
                TransactionAttributeType.OracleResponse => new OracleResponse(),
                _ => throw new FormatException(),
            };

        public abstract int Size { get; }
        public abstract void Serialize(BinaryWriter writer);
        protected abstract void Deserialize(TransactionAttributeType type, BinaryReader reader);
        public abstract JObject ToJson();
        protected abstract void DeserializeJson(TransactionAttributeType type, JObject json);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            throw new InvalidOperationException();
        }
    }
}
