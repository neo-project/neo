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

        public virtual Fixed8 SystemFee => Fixed8.Zero;

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

        internal IDictionary<TransactionInput, TransactionOutput> GetReferences()
        {
            return GetReferences(GetAllInputs());
        }

        internal static IDictionary<TransactionInput, TransactionOutput> GetReferences(IEnumerable<TransactionInput> inputs)
        {
            Dictionary<TransactionInput, TransactionOutput> references = new Dictionary<TransactionInput, TransactionOutput>();
            foreach (var group in inputs.GroupBy(p => p.PrevTxId))
            {
                Transaction tx = Blockchain.Default.GetTransaction(group.Key);
                if (tx == null) throw new InvalidOperationException();
                foreach (var reference in group.Select(p => new
                {
                    Input = p,
                    Output = tx.Outputs[p.PrevIndex]
                }))
                {
                    references.Add(reference.Input, reference.Output);
                }
            }
            return references;
        }

        public virtual UInt160[] GetScriptHashesForVerifying()
        {
            return GetReferences(Inputs).Values.Select(p => p.ScriptHash).Distinct().OrderBy(p => p).ToArray();
        }

        internal IDictionary<UInt256, TransactionResult> GetTransactionResults()
        {
            IDictionary<TransactionInput, TransactionOutput> references = GetUnspentReferences();
            return references.Values.Select(p => new
            {
                AssetId = p.AssetId,
                Value = p.Value
            }).Concat(Outputs.Select(p => new
            {
                AssetId = p.AssetId,
                Value = -p.Value
            })).GroupBy(p => p.AssetId, (k, g) => new TransactionResult
            {
                AssetId = k,
                Amount = g.Sum(p => p.Value)
            }).Where(p => p.Amount != Fixed8.Zero).ToDictionary(p => p.AssetId);
        }

        internal IDictionary<TransactionInput, TransactionOutput> GetUnspentReferences()
        {
            Dictionary<TransactionInput, TransactionOutput> references = new Dictionary<TransactionInput, TransactionOutput>();
            foreach (TransactionInput input in GetAllInputs())
            {
                TransactionOutput reference = Blockchain.Default.GetUnspent(input.PrevTxId, input.PrevIndex);
                if (reference == null) throw new InvalidOperationException();
                references.Add(input, reference);
            }
            return references;
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
            if (Outputs.Any(p => p.Value <= Fixed8.Zero))
                return false;
            IDictionary<UInt256, TransactionResult> results = GetTransactionResults();
            if (results.Count > 1) return false;
            if (results.Count == 1 && !results.ContainsKey(Blockchain.AntCoin.Hash))
                return false;
            if (SystemFee == Fixed8.Zero) return true;
            if (results.Count == 0 || results[Blockchain.AntCoin.Hash].Amount < SystemFee)
                return false;
            return true;
        }
    }
}
