using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    /// <summary>
    /// 部分成交的订单
    /// </summary>
    public class SplitOrder : ISerializable
    {
        /// <summary>
        /// 买入或卖出的数量
        /// </summary>
        public Fixed8 Amount;
        /// <summary>
        /// 价格
        /// </summary>
        public Fixed8 Price;
        /// <summary>
        /// 委托人的合约散列
        /// </summary>
        public UInt160 Client;

        public int Size => Amount.Size + Price.Size + Client.Size;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            this.Amount = reader.ReadSerializable<Fixed8>();
            if (Amount == Fixed8.Zero) throw new FormatException();
            if (Amount.GetData() % 10000 != 0) throw new FormatException();
            this.Price = reader.ReadSerializable<Fixed8>();
            if (Price <= Fixed8.Zero) throw new FormatException();
            if (Price.GetData() % 10000 != 0) throw new FormatException();
            this.Client = reader.ReadSerializable<UInt160>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Amount);
            writer.Write(Price);
            writer.Write(Client);
        }
    }
}
