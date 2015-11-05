using AntShares.Core.Scripts;
using AntShares.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class Order : ISignable
    {
        public UInt256 AssetId;
        public UInt256 ValueAssetId;
        public UInt160 Agent;
        /// <summary>
        /// 买入或卖出的数量，正数表示买入，负数表示卖出
        /// </summary>
        public Fixed8 Amount;
        public Fixed8 Price;
        public UInt160 Client;
        public TransactionInput[] Inputs;
        public Script[] Scripts;

        Script[] ISignable.Scripts
        {
            get
            {
                return Scripts;
            }
            set
            {
                Scripts = value;
            }
        }

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
