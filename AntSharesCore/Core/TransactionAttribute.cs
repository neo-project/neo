using AntShares.IO;
using AntShares.IO.Json;
using System;
using System.IO;

namespace AntShares.Core
{
    public class TransactionAttribute : ISerializable
    {
        public TransactionAttributeUsage Usage;
        public byte[] Data;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Usage = (TransactionAttributeUsage)reader.ReadByte();
            int length;
            switch (Usage)
            {
                case TransactionAttributeUsage.ContractHash:
                case TransactionAttributeUsage.ECDH02:
                case TransactionAttributeUsage.ECDH03:
                    length = 32;
                    break;
                case TransactionAttributeUsage.Remark:
                case TransactionAttributeUsage.Script:
                    length = reader.ReadByte();
                    break;
                default:
                    throw new FormatException();
            }
            Data = reader.ReadBytes(length);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Usage);
            if (Usage >= TransactionAttributeUsage.Remark)
                writer.Write((byte)Data.Length);
            writer.Write(Data);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["usage"] = Usage;
            json["data"] = Data.ToHexString();
            return json;
        }
    }
}
