using AntShares.IO;
using AntShares.IO.Json;
using AntShares.Wallets;
using System;
using System.IO;

namespace AntShares.Core
{
    /// <summary>
    /// 交易输出
    /// </summary>
    public class TransactionOutput : ISerializable
    {
        /// <summary>
        /// 资产编号
        /// </summary>
        public UInt256 AssetId;
        /// <summary>
        /// 金额
        /// </summary>
        public Fixed8 Value;
        /// <summary>
        /// 收款地址
        /// </summary>
        public UInt160 ScriptHash;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            this.AssetId = reader.ReadSerializable<UInt256>();
            this.Value = reader.ReadSerializable<Fixed8>();
            if (Value <= Fixed8.Zero) throw new FormatException();
            this.ScriptHash = reader.ReadSerializable<UInt160>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(AssetId);
            writer.Write(Value);
            writer.Write(ScriptHash);
        }

        /// <summary>
        /// 将交易输出转变为json对象
        /// </summary>
        /// <param name="index">该交易输出在交易中的索引</param>
        /// <returns>返回json对象</returns>
        public JObject ToJson(ushort index)
        {
            JObject json = new JObject();
            json["n"] = index;
            json["asset"] = AssetId.ToString();
            json["value"] = Value.ToString();
            json["high"] = Value.GetData() >> 32;
            json["low"] = Value.GetData() & 0xffffffff;
            json["address"] = Wallet.ToAddress(ScriptHash);
            return json;
        }
    }
}
