using AntShares.Cryptography;
using AntShares.IO;
using AntShares.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AntShares.Core
{
    public abstract class Transaction : ISignable
    {
        public readonly TransactionType Type;
        public TransactionInput[] Inputs;
        public TransactionOutput[] Outputs;
        public byte[][] Scripts;

        private UInt256 hash = null;

        public UInt256 Hash
        {
            get
            {
                if (hash == null)
                {
                    hash = new UInt256(this.ToArray().Sha256().Sha256());
                }
                return hash;
            }
        }

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

        protected Transaction(TransactionType type)
        {
            this.Type = type;
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if ((TransactionType)reader.ReadByte() != Type)
                throw new FormatException();
            DeserializeWithoutType(reader);
        }

        protected abstract void DeserializeExclusiveData(BinaryReader reader);

        public static Transaction DeserializeFrom(byte[] value)
        {
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return DeserializeFrom(reader);
            }
        }

        internal static Transaction DeserializeFrom(BinaryReader reader)
        {
            TransactionType type = (TransactionType)reader.ReadByte();
            string typeName = string.Format("{0}.{1}", typeof(Transaction).Namespace, type);
            Transaction transaction = typeof(Transaction).Assembly.CreateInstance(typeName) as Transaction;
            if (transaction == null)
                throw new FormatException();
            transaction.DeserializeWithoutType(reader);
            return transaction;
        }

        private void DeserializeWithoutType(BinaryReader reader)
        {
            DeserializeExclusiveData(reader);
            this.Inputs = reader.ReadSerializableArray<TransactionInput>();
            if (GetAllInputs().Distinct().Count() != GetAllInputs().Count())
                throw new FormatException();
            this.Outputs = reader.ReadSerializableArray<TransactionOutput>();
            this.Scripts = reader.ReadBytesArray();
        }

        void ISignable.FromUnsignedArray(byte[] value)
        {
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                if ((TransactionType)reader.ReadByte() != Type)
                    throw new FormatException();
                DeserializeExclusiveData(reader);
                this.Inputs = reader.ReadSerializableArray<TransactionInput>();
                this.Outputs = reader.ReadSerializableArray<TransactionOutput>();
            }
        }

        byte[] ISignable.GetHashForSigning()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write((byte)Type);
                SerializeExclusiveData(writer);
                writer.Write(Inputs);
                writer.Write(Outputs);
                writer.Flush();
                return ms.ToArray().Sha256();
            }
        }

        public virtual UInt160[] GetScriptHashesForVerifying()
        {
            if (Inputs.Length == 0) return new UInt160[0];
            HashSet<UInt160> hashes = new HashSet<UInt160>();
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
            writer.Write((byte)Type);
            SerializeExclusiveData(writer);
            writer.Write(Inputs);
            writer.Write(Outputs);
            writer.Write(Scripts);
        }

        public virtual IEnumerable<TransactionInput> GetAllInputs()
        {
            return Inputs;
        }

        protected abstract void SerializeExclusiveData(BinaryWriter writer);

        byte[] ISignable.ToUnsignedArray()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write((byte)Type);
                SerializeExclusiveData(writer);
                writer.Write(Inputs);
                writer.Write(Outputs);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public virtual bool Verify()
        {
            if (GetAllInputs().Distinct().Count() != GetAllInputs().Count())
                return false;
            lock (LocalNode.MemoryPool)
            {
                if (LocalNode.MemoryPool.Values.AsParallel().SelectMany(p => p.GetAllInputs()).Intersect(GetAllInputs().AsParallel()).Count() > 0)
                    return false;
            }
            if (!VerifyBalance()) return false;
            if (!this.VerifySignature()) return false;
            return true;
        }

        internal virtual bool VerifyBalance()
        {
            if (Outputs.Any(p => p.Value <= 0))
                return false;
            List<TransactionOutput> unspent_coins = new List<TransactionOutput>();
            foreach (TransactionInput input in GetAllInputs())
            {
                TransactionOutput unspent = Blockchain.Default.GetUnspent(input.PrevTxId, input.PrevIndex);
                if (unspent == null) return false;
                unspent_coins.Add(unspent);
            }
            var inputs = unspent_coins.GroupBy(p => p.AssetId, (k, g) => new
            {
                AssetId = k,
                Amount = g.Sum(p => p.Value)
            }).Where(p => p.Amount != 0).ToArray();
            var outputs = Outputs.GroupBy(p => p.AssetId, (k, g) => new
            {
                AssetId = k,
                Amount = g.Sum(p => p.Value)
            }).Where(p => p.Amount != 0).ToDictionary(p => p.AssetId);
            if (inputs.Length < outputs.Count || inputs.Length > outputs.Count + 1)
                return false;
            foreach (var input in inputs)
            {
                if (outputs.ContainsKey(input.AssetId))
                {
                    if (input.AssetId == Blockchain.AntCoin.Hash)
                    {
                        if (input.Amount < outputs[input.AssetId].Amount)
                            return false;
                    }
                    else
                    {
                        if (input.Amount != outputs[input.AssetId].Amount)
                            return false;
                    }
                }
                else
                {
                    if (input.AssetId != Blockchain.AntCoin.Hash)
                        return false;
                }
            }
            return true;
        }
    }
}
