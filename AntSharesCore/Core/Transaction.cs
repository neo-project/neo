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
    public abstract class Transaction : Inventory, ISignable
    {
        public readonly TransactionType Type;
        public TransactionInput[] Inputs;
        public TransactionOutput[] Outputs;
        public byte[][] Scripts;

        public override InventoryType InventoryType
        {
            get
            {
                return InventoryType.TX;
            }
        }

        [NonSerialized]
        private IReadOnlyDictionary<TransactionInput, TransactionOutput> _references;
        public IReadOnlyDictionary<TransactionInput, TransactionOutput> References
        {
            get
            {
                if (_references == null)
                {
                    Dictionary<TransactionInput, TransactionOutput> dictionary = new Dictionary<TransactionInput, TransactionOutput>();
                    foreach (var group in GetAllInputs().GroupBy(p => p.PrevTxId))
                    {
                        Transaction tx = Blockchain.Default.GetTransaction(group.Key);
                        if (tx == null) return null;
                        foreach (var reference in group.Select(p => new
                        {
                            Input = p,
                            Output = tx.Outputs[p.PrevIndex]
                        }))
                        {
                            dictionary.Add(reference.Input, reference.Output);
                        }
                    }
                    _references = dictionary;
                }
                return _references;
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

        public override void Deserialize(BinaryReader reader)
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
            if (Outputs.Any(p => p.Value == Fixed8.Zero))
                throw new FormatException();
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
            if (References == null) throw new InvalidOperationException();
            TransactionOutput[] inputs = Inputs.Select(p => References[p]).ToArray();
            HashSet<UInt160> hashes = new HashSet<UInt160>(inputs.Where(p => p.Value > Fixed8.Zero).Select(p => p.ScriptHash));
            foreach (UInt256 asset_id in inputs.Where(p => p.Value < Fixed8.Zero).Select(p => p.AssetId).Distinct())
            {
                RegisterTransaction tx = Blockchain.Default.GetTransaction(asset_id) as RegisterTransaction;
                if (tx == null) throw new InvalidOperationException();
                hashes.Add(tx.Admin);
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        public IEnumerable<TransactionResult> GetTransactionResults()
        {
            if (References == null) return null;
            return References.Values.Select(p => new
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
            }).Where(p => p.Amount != Fixed8.Zero);
        }

        protected virtual void OnDeserialized()
        {
        }

        public override void Serialize(BinaryWriter writer)
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

        public override VerificationResult Verify()
        {
            if (Blockchain.Default.ContainsTransaction(Hash)) return VerificationResult.AlreadyInBlockchain;
            VerificationResult result = VerificationResult.OK;
            if (Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
            {
                if (Blockchain.Default.IsDoubleSpend(this))
                    result |= VerificationResult.DoubleSpent;
            }
            else
            {
                result |= VerificationResult.Incapable;
            }
            if (Blockchain.Default.Ability.HasFlag(BlockchainAbility.TransactionIndexes))
            {
                foreach (UInt256 hash in Outputs.Select(p => p.AssetId).Distinct())
                {
                    if (!Blockchain.Default.ContainsAsset(hash))
                    {
                        result |= VerificationResult.LackOfInformation;
                        break;
                    }
                }
            }
            else
            {
                result |= VerificationResult.Incapable;
            }
            if (References == null)
            {
                result |= VerificationResult.LackOfInformation;
            }
            else
            {
                foreach (var group in Outputs.Where(p => p.Value < Fixed8.Zero).GroupBy(p => p.AssetId))
                {
                    if (group.Key == Blockchain.AntCoin.Hash || group.Key == Blockchain.AntShare.Hash)
                    {
                        result |= VerificationResult.Imbalanced;
                        break;
                    }
                    RegisterTransaction tx = Blockchain.Default.GetTransaction(group.Key) as RegisterTransaction;
                    if (tx == null)
                    {
                        result |= VerificationResult.LackOfInformation;
                        continue;
                    }
                    if (tx.Amount != Fixed8.Zero)
                    {
                        result |= VerificationResult.Imbalanced;
                        break;
                    }
                    if (group.Any(p => p.ScriptHash != tx.Issuer))
                    {
                        result |= VerificationResult.Imbalanced;
                        break;
                    }
                    if (Type != TransactionType.IssueTransaction && References.Values.Where(p => p.AssetId == group.Key && p.Value < Fixed8.Zero).Sum(p => p.Value) > group.Sum(p => p.Value))
                    {
                        result |= VerificationResult.Imbalanced;
                        break;
                    }
                }
                TransactionResult[] results = GetTransactionResults().ToArray();
                TransactionResult[] results_destroy = results.Where(p => p.Amount > Fixed8.Zero).ToArray();
                if (results_destroy.Length > 1)
                    result |= VerificationResult.Imbalanced;
                else if (results_destroy.Length == 1 && results_destroy[0].AssetId != Blockchain.AntCoin.Hash)
                    result |= VerificationResult.Imbalanced;
                else if (SystemFee > Fixed8.Zero && (results_destroy.Length == 0 || results_destroy[0].Amount < SystemFee))
                    result |= VerificationResult.Imbalanced;
                TransactionResult[] results_issue = results.Where(p => p.Amount < Fixed8.Zero).ToArray();
                if (Type == TransactionType.GenerationTransaction)
                {
                    if (results_issue.Any(p => p.AssetId != Blockchain.AntCoin.Hash))
                        result |= VerificationResult.Imbalanced;
                }
                else if (Type != TransactionType.IssueTransaction)
                {
                    result |= VerificationResult.Imbalanced;
                }
            }
            result |= this.VerifySignature();
            return result;
        }
    }
}
