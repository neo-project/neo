using System;
using System.IO;
using Neo.IO;

namespace Neo.Models
{
    public abstract class TransactionAttribute
    {
        public abstract bool AllowMultiple { get; }

        public static TransactionAttribute DeserializeFrom(BinaryReader reader)
        {
            TransactionAttributeType type = (TransactionAttributeType)reader.ReadByte();
            TransactionAttribute attribute = type switch
            {
                TransactionAttributeType.HighPriority => new HighPriorityAttribute(),
                TransactionAttributeType.OracleResponse => new OracleResponse(),
                _ => throw new FormatException(),
            };
            attribute.Deserialize(type, reader);
            return attribute;
        }

        public abstract int Size { get; }

        public abstract void Serialize(BinaryWriter writer);
        protected abstract void Deserialize(TransactionAttributeType type, BinaryReader reader);
    }
}
