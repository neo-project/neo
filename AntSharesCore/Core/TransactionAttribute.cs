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
            if (Usage == TransactionAttributeUsage.ContractHash || (Usage >= TransactionAttributeUsage.Hash1 && Usage <= TransactionAttributeUsage.Hash15))
                length = 32;
            else if (Usage == TransactionAttributeUsage.ECDH02 || Usage == TransactionAttributeUsage.ECDH03)
                length = 32;
            else if (Usage == TransactionAttributeUsage.Script)
                length = (int)reader.ReadVarInt(ushort.MaxValue);
            else if (Usage >= TransactionAttributeUsage.Remark)
                length = reader.ReadByte();
            else
                throw new FormatException();
            Data = reader.ReadBytes(length);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Usage);
            if (Usage == TransactionAttributeUsage.Script)
                writer.WriteVarInt(Data.Length);
            else if (Usage >= TransactionAttributeUsage.Remark)
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
