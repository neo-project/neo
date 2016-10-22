using AntShares.IO;
using AntShares.IO.Json;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    /// <summary>
    /// 交易特性
    /// </summary>
    public class TransactionAttribute : ISerializable
    {
        /// <summary>
        /// 用途
        /// </summary>
        public TransactionAttributeUsage Usage;
        /// <summary>
        /// 特定于用途的外部数据
        /// </summary>
        public byte[] Data;

        public int Size
        {
            get
            {
                if (Usage == TransactionAttributeUsage.ContractHash || Usage == TransactionAttributeUsage.ECDH02 || Usage == TransactionAttributeUsage.ECDH03 || Usage == TransactionAttributeUsage.Vote || (Usage >= TransactionAttributeUsage.Hash1 && Usage <= TransactionAttributeUsage.Hash15))
                    return sizeof(TransactionAttributeUsage) + 32;
                else
                    return sizeof(TransactionAttributeUsage) + Data.Length.GetVarSize() + Data.Length;
            }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Usage = (TransactionAttributeUsage)reader.ReadByte();
            if (Usage == TransactionAttributeUsage.ContractHash || Usage == TransactionAttributeUsage.Vote || (Usage >= TransactionAttributeUsage.Hash1 && Usage <= TransactionAttributeUsage.Hash15))
                Data = reader.ReadBytes(32);
            else if (Usage == TransactionAttributeUsage.ECDH02 || Usage == TransactionAttributeUsage.ECDH03)
                Data = new[] { (byte)Usage }.Concat(reader.ReadBytes(32)).ToArray();
            else if (Usage == TransactionAttributeUsage.Script)
                Data = reader.ReadVarBytes(ushort.MaxValue);
            else if (Usage == TransactionAttributeUsage.CertUrl || Usage == TransactionAttributeUsage.DescriptionUrl)
                Data = reader.ReadVarBytes(byte.MaxValue);
            else if (Usage == TransactionAttributeUsage.Description || Usage >= TransactionAttributeUsage.Remark)
                Data = reader.ReadVarBytes(byte.MaxValue);
            else
                throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Usage);
            if (Usage == TransactionAttributeUsage.Script)
                writer.WriteVarInt(Data.Length);
            else if (Usage == TransactionAttributeUsage.CertUrl || Usage == TransactionAttributeUsage.DescriptionUrl)
                writer.Write((byte)Data.Length);
            else if (Usage == TransactionAttributeUsage.Description || Usage >= TransactionAttributeUsage.Remark)
                writer.Write((byte)Data.Length);
            if (Usage == TransactionAttributeUsage.ECDH02 || Usage == TransactionAttributeUsage.ECDH03)
                writer.Write(Data, 1, 32);
            else
                writer.Write(Data);
        }

        /// <summary>
        /// 变成json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public JObject ToJson()
        {
            JObject json = new JObject();
            json["usage"] = Usage;
            json["data"] = Data.ToHexString();
            return json;
        }
    }
}
