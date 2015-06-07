using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    public class Order : ISerializable, ISignable
    {
        public const byte OrderType = 0;
        public UInt256 AssetType;
        public UInt256 ValueType;
        public Int64 Amount;
        public UInt64 Price;
        public UInt160 ScriptHash;
        public UInt160 Agent;
        public TransactionInput[] Inputs;
        public byte[][] Scripts;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != OrderType)
                throw new FormatException();
            this.AssetType = reader.ReadSerializable<UInt256>();
            this.ValueType = reader.ReadSerializable<UInt256>();
            this.Amount = reader.ReadInt64();
            this.Price = reader.ReadUInt64();
            this.ScriptHash = reader.ReadSerializable<UInt160>();
            this.Agent = reader.ReadSerializable<UInt160>();
            this.Inputs = reader.ReadSerializableArray<TransactionInput>();
            this.Scripts = reader.ReadBytesArray();
        }

        byte[] ISignable.GetHashForSigning()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(OrderType);
                writer.Write(AssetType);
                writer.Write(ValueType);
                writer.Write(Amount);
                writer.Write(Price);
                writer.Write(ScriptHash);
                writer.Write(Agent);
                writer.Write(Inputs);
                writer.Flush();
                return ms.ToArray().Sha256();
            }
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            //TODO: 列出需要对订单签名的地址列表
            //1. 所有的输入地址
            //2. 如果订单中购买或售出的资产是股权，那么输出ScriptHash也需要对订单签名
            //需要本地区块链数据库，否则无法验证
            //3. 无法验证的情况下，抛出异常：
            //throw new InvalidOperationException();

            throw new NotImplementedException();
        }

        byte[][] ISignable.GetScriptsForVerifying()
        {
            return Scripts;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(OrderType);
            writer.Write(AssetType);
            writer.Write(ValueType);
            writer.Write(Amount);
            writer.Write(Price);
            writer.Write(ScriptHash);
            writer.Write(Agent);
            writer.Write(Inputs);
            writer.Write(Scripts);
        }

        byte[] ISignable.ToUnsignedArray()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(OrderType);
                writer.Write(AssetType);
                writer.Write(ValueType);
                writer.Write(Amount);
                writer.Write(Price);
                writer.Write(ScriptHash);
                writer.Write(Agent);
                writer.Write(Inputs);
                writer.Flush();
                return ms.ToArray();
            }
        }
    }
}
