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
        public const byte OrderType = 0;
        public UInt256 AssetId;
        public UInt256 ValueAssetId;
        public Int64 Amount;
        public UInt64 Price;
        public UInt160 ScriptHash;
        public UInt160 Agent;
        public TransactionInput[] Inputs;

        public byte[][] Scripts { get; set; }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != OrderType)
                throw new FormatException();
            this.AssetId = reader.ReadSerializable<UInt256>();
            this.ValueAssetId = reader.ReadSerializable<UInt256>();
            this.Amount = reader.ReadInt64();
            this.Price = reader.ReadUInt64();
            this.ScriptHash = reader.ReadSerializable<UInt160>();
            this.Agent = reader.ReadSerializable<UInt160>();
            this.Inputs = reader.ReadSerializableArray<TransactionInput>();
            this.Scripts = reader.ReadBytesArray();
        }

        void ISignable.FromUnsignedArray(byte[] value)
        {
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                if (reader.ReadByte() != OrderType)
                    throw new FormatException();
                this.AssetId = reader.ReadSerializable<UInt256>();
                this.ValueAssetId = reader.ReadSerializable<UInt256>();
                this.Amount = reader.ReadInt64();
                this.Price = reader.ReadUInt64();
                this.ScriptHash = reader.ReadSerializable<UInt160>();
                this.Agent = reader.ReadSerializable<UInt160>();
                this.Inputs = reader.ReadSerializableArray<TransactionInput>();
            }
        }

        byte[] ISignable.GetHashForSigning()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(OrderType);
                writer.Write(AssetId);
                writer.Write(ValueAssetId);
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
            Blockchain blockchain = Blockchain.Default;
            if (blockchain == null) throw new InvalidOperationException();
            HashSet<UInt160> hashes = new HashSet<UInt160>();
            RegisterTransaction asset = blockchain.GetTransaction(AssetId) as RegisterTransaction;
            if (asset == null) throw new InvalidOperationException();
            if (asset.RegisterType == RegisterType.Share)
            {
                hashes.Add(ScriptHash);
            }
            foreach (var group in Inputs.GroupBy(p => p.PrevTxId))
            {
                Transaction tx = blockchain.GetTransaction(group.Key);
                if (tx == null) throw new InvalidOperationException();
                hashes.UnionWith(group.Select(p => tx.Outputs[p.PrevIndex].ScriptHash));
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(OrderType);
            writer.Write(AssetId);
            writer.Write(ValueAssetId);
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
                writer.Write(AssetId);
                writer.Write(ValueAssetId);
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
