using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class Order : ISignable
    {
        /// <summary>
        /// 订单类型，目前只支持限价委托单
        /// </summary>
        public const byte Type = 0;
        public UInt256 AssetId;
        public UInt256 ValueAssetId;
        /// <summary>
        /// 买入或卖出的数量，正数表示买入，负数表示卖出
        /// </summary>
        public Fixed8 Amount;
        public Fixed8 Price;
        public UInt160 Client;
        public UInt160 Agent;
        public TransactionInput[] Inputs;
        public byte[][] Scripts;

        byte[][] ISignable.Scripts
        {
            get
            {
                return this.Scripts;
            }
            set
            {
                this.Scripts = value;
            }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != Type)
                throw new FormatException();
            this.AssetId = reader.ReadSerializable<UInt256>();
            this.ValueAssetId = reader.ReadSerializable<UInt256>();
            if (AssetId == ValueAssetId) throw new FormatException();
            this.Amount = reader.ReadFixed8();
            if (Amount == Fixed8.Zero) throw new FormatException();
            if (Amount.GetData() % 1000 != 0) throw new FormatException(); //订单中交易物的数量最多保留5位小数
            this.Price = reader.ReadFixed8();
            if (Price <= Fixed8.Zero) throw new FormatException();
            if (Price.GetData() % 100000 != 0) throw new FormatException(); //订单中的价格最多保留3位小数
            this.Client = reader.ReadSerializable<UInt160>();
            this.Agent = reader.ReadSerializable<UInt160>();
            this.Inputs = reader.ReadSerializableArray<TransactionInput>();
            if (Inputs.Distinct().Count() != Inputs.Length)
                throw new FormatException();
            this.Scripts = reader.ReadBytesArray();
        }

        void ISignable.FromUnsignedArray(byte[] value)
        {
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                if (reader.ReadByte() != Type)
                    throw new FormatException();
                this.AssetId = reader.ReadSerializable<UInt256>();
                this.ValueAssetId = reader.ReadSerializable<UInt256>();
                this.Amount = reader.ReadFixed8();
                this.Price = reader.ReadFixed8();
                this.Client = reader.ReadSerializable<UInt160>();
                this.Agent = reader.ReadSerializable<UInt160>();
                this.Inputs = reader.ReadSerializableArray<TransactionInput>();
            }
        }

        byte[] ISignable.GetHashForSigning()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(Type);
                writer.Write(AssetId);
                writer.Write(ValueAssetId);
                writer.Write(Amount);
                writer.Write(Price);
                writer.Write(Client);
                writer.Write(Agent);
                writer.Write(Inputs);
                writer.Flush();
                return ms.ToArray().Sha256();
            }
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>();
            RegisterTransaction asset = Blockchain.Default.GetTransaction(AssetId) as RegisterTransaction;
            if (asset == null) throw new InvalidOperationException();
            if (asset.AssetType == AssetType.Share)
            {
                hashes.Add(Client);
            }
            foreach (var group in Inputs.GroupBy(p => p.PrevTxId))
            {
                Transaction tx = Blockchain.Default.GetTransaction(group.Key);
                if (tx == null) throw new InvalidOperationException();
                hashes.UnionWith(group.Select(p => tx.Outputs[p.PrevIndex].ScriptHash));
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Type);
            writer.Write(AssetId);
            writer.Write(ValueAssetId);
            writer.Write(Amount);
            writer.Write(Price);
            writer.Write(Client);
            writer.Write(Agent);
            writer.Write(Inputs);
            writer.Write(Scripts);
        }

        byte[] ISignable.ToUnsignedArray()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(Type);
                writer.Write(AssetId);
                writer.Write(ValueAssetId);
                writer.Write(Amount);
                writer.Write(Price);
                writer.Write(Client);
                writer.Write(Agent);
                writer.Write(Inputs);
                writer.Flush();
                return ms.ToArray();
            }
        }
    }
}
