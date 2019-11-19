using Neo.IO;
using Neo.IO.Json;
using Neo.Oracle;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public abstract class TransactionAttribute : ISerializable
    {
        public readonly TransactionAttributeUsage Usage;

        public virtual int Size => sizeof(TransactionAttributeUsage);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="usage">Usage</param>
        protected TransactionAttribute(TransactionAttributeUsage usage)
        {
            Usage = usage;
        }

        protected abstract void DeserializeWithoutType(BinaryReader reader);
        protected abstract void SerializeWithoutType(BinaryWriter writer);

        public void Deserialize(BinaryReader reader)
        {
            var usage = (TransactionAttributeUsage)reader.ReadByte();
            if (Usage != usage) throw new FormatException();
            DeserializeWithoutType(reader);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Usage);
            SerializeWithoutType(writer);
        }

        public virtual JObject ToJson()
        {
            JObject json = new JObject();
            json["usage"] = Usage;
            return json;
        }

        public static TransactionAttribute FromJson(JObject json)
        {
            var usage = (TransactionAttributeUsage)(byte.Parse(json["usage"].AsString()));

            return usage switch
            {
                TransactionAttributeUsage.OracleExpectedResult => OracleExpectedResult.FromJson(json),
                _ => throw new FormatException(),
            };
        }

        public static TransactionAttribute DeserializeFrom(BinaryReader reader)
        {
            var usage = (TransactionAttributeUsage)reader.ReadByte();

            TransactionAttribute attrib;
            switch (usage)
            {
                case TransactionAttributeUsage.OracleExpectedResult:
                    {
                        attrib = new OracleExpectedResult();
                        break;
                    }
                default: throw new FormatException();
            }
            attrib.DeserializeWithoutType(reader);
            return attrib;
        }
    }
}
