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
            UInt256 asset_id = reader.ReadSerializable<UInt256>();
            UInt256 value_asset_id = reader.ReadSerializable<UInt256>();
            if (asset_id == value_asset_id) throw new FormatException();
            UInt160 agent = reader.ReadSerializable<UInt160>();
            DeserializeInternal(reader, asset_id, value_asset_id, agent);
        }

        internal void DeserializeInTransaction(BinaryReader reader, AgencyTransaction tx)
        {
            DeserializeInternal(reader, tx.AssetId, tx.ValueAssetId, tx.Agent);
        }

        private void DeserializeInternal(BinaryReader reader, UInt256 asset_id, UInt256 value_asset_id, UInt160 agent)
        {
            this.AssetId = asset_id;
            this.ValueAssetId = value_asset_id;
            this.Agent = agent;
            this.Amount = reader.ReadSerializable<Fixed8>();
            if (Amount == Fixed8.Zero) throw new FormatException();
            if (Amount.GetData() % 10000 != 0) throw new FormatException();
            this.Price = reader.ReadSerializable<Fixed8>();
            if (Price <= Fixed8.Zero) throw new FormatException();
            if (Price.GetData() % 10000 != 0) throw new FormatException();
            this.Client = reader.ReadSerializable<UInt160>();
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
                this.AssetId = reader.ReadSerializable<UInt256>();
                this.ValueAssetId = reader.ReadSerializable<UInt256>();
                this.Agent = reader.ReadSerializable<UInt160>();
                this.Amount = reader.ReadSerializable<Fixed8>();
                this.Price = reader.ReadSerializable<Fixed8>();
                this.Client = reader.ReadSerializable<UInt160>();
                this.Inputs = reader.ReadSerializableArray<TransactionInput>();
            }
        }

        byte[] ISignable.GetHashForSigning()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(AssetId);
                writer.Write(ValueAssetId);
                writer.Write(Agent);
                writer.Write(Amount);
                writer.Write(Price);
                writer.Write(Client);
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
            writer.Write(AssetId);
            writer.Write(ValueAssetId);
            writer.Write(Agent);
            writer.Write(Amount);
            writer.Write(Price);
            writer.Write(Client);
            writer.Write(Inputs);
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

        byte[] ISignable.ToUnsignedArray()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(AssetId);
                writer.Write(ValueAssetId);
                writer.Write(Agent);
                writer.Write(Amount);
                writer.Write(Price);
                writer.Write(Client);
                writer.Write(Inputs);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public VerificationResult Verify()
        {
            return this.VerifySignature();
        }
    }
}
