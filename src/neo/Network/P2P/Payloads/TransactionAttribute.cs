using Neo.IO;
using Neo.IO.Caching;
using Neo.IO.Json;
using Neo.Persistence;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public abstract class TransactionAttribute : ISerializable
    {
        public abstract TransactionAttributeType Type { get; }
        public abstract bool AllowMultiple { get; }
        public virtual int Size => sizeof(TransactionAttributeType);

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != (byte)Type)
                throw new FormatException();
            DeserializeWithoutType(reader);
        }

        public static TransactionAttribute DeserializeFrom(BinaryReader reader)
        {
            TransactionAttributeType type = (TransactionAttributeType)reader.ReadByte();
            if (!(ReflectionCache<TransactionAttributeType>.CreateInstance(type) is TransactionAttribute attribute))
                throw new FormatException();
            attribute.DeserializeWithoutType(reader);
            return attribute;
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

        public virtual bool Verify(StoreView snapshot, Transaction tx) => true;
    }
}
