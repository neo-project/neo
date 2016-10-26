using AntShares.IO;
using AntShares.VM;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    /// <summary>
    /// 订单
    /// </summary>
    public class Order : ISignable
    {
        /// <summary>
        /// 资产编号
        /// </summary>
        public UInt256 AssetId;
        /// <summary>
        /// 货币编号
        /// </summary>
        public UInt256 ValueAssetId;
        /// <summary>
        /// 代理人的合约散列
        /// </summary>
        public UInt160 Agent;
        /// <summary>
        /// 买入或卖出的数量，正数表示买入，负数表示卖出
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
        /// <summary>
        /// 输入列表
        /// </summary>
        public TransactionInput[] Inputs;
        /// <summary>
        /// 用于验证该订单的脚本列表
        /// </summary>
        public Script[] Scripts { get; set; }

        public int Size => AssetId.Size + ValueAssetId.Size + Agent.Size + SizeInTransaction;

        public int SizeInTransaction => Amount.Size + Price.Size + Client.Size + Inputs.Length.GetVarSize() + Inputs.Sum(p => p.Size) + Scripts.Length.GetVarSize() + Scripts.Sum(p => p.Size);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((ISignable)this).DeserializeUnsigned(reader);
            Scripts = reader.ReadSerializableArray<Script>();
        }

        internal void DeserializeInTransaction(BinaryReader reader, AgencyTransaction tx)
        {
            DeserializeUnsignedInternal(reader, tx.AssetId, tx.ValueAssetId, tx.Agent);
            Scripts = reader.ReadSerializableArray<Script>();
        }

        void ISignable.DeserializeUnsigned(BinaryReader reader)
        {
            UInt256 asset_id = reader.ReadSerializable<UInt256>();
            UInt256 value_asset_id = reader.ReadSerializable<UInt256>();
            if (asset_id == value_asset_id) throw new FormatException();
            UInt160 agent = reader.ReadSerializable<UInt160>();
            DeserializeUnsignedInternal(reader, asset_id, value_asset_id, agent);
        }

        private void DeserializeUnsignedInternal(BinaryReader reader, UInt256 asset_id, UInt256 value_asset_id, UInt160 agent)
        {
            AssetId = asset_id;
            ValueAssetId = value_asset_id;
            Agent = agent;
            Amount = reader.ReadSerializable<Fixed8>();
            if (Amount == Fixed8.Zero) throw new FormatException();
            if (Amount.GetData() % 10000 != 0) throw new FormatException();
            Price = reader.ReadSerializable<Fixed8>();
            if (Price <= Fixed8.Zero) throw new FormatException();
            if (Price.GetData() % 10000 != 0) throw new FormatException();
            Client = reader.ReadSerializable<UInt160>();
            Inputs = reader.ReadSerializableArray<TransactionInput>();
            if (Inputs.Distinct().Count() != Inputs.Length)
                throw new FormatException();
        }

        byte[] ISignableObject.GetMessage()
        {
            return this.GetHashData();
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
            foreach (var group in Inputs.GroupBy(p => p.PrevHash))
            {
                Transaction tx = Blockchain.Default.GetTransaction(group.Key);
                if (tx == null) throw new InvalidOperationException();
                hashes.UnionWith(group.Select(p => tx.Outputs[p.PrevIndex].ScriptHash));
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((ISignable)this).SerializeUnsigned(writer);
            writer.Write(Scripts);
        }

        internal void SerializeInTransaction(BinaryWriter writer)
        {
            writer.Write(Amount);
            writer.Write(Price);
            writer.Write(Client);
            writer.Write(Inputs);
            writer.Write(Scripts);
        }

        void ISignable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(AssetId);
            writer.Write(ValueAssetId);
            writer.Write(Agent);
            writer.Write(Amount);
            writer.Write(Price);
            writer.Write(Client);
            writer.Write(Inputs);
        }
    }
}
