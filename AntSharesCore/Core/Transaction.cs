using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.IO.Json;
using AntShares.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AntShares.Core
{
    /// <summary>
    /// 一切交易的基类
    /// </summary>
    public abstract class Transaction : IInventory
    {
        /// <summary>
        /// 交易类型
        /// </summary>
        public readonly TransactionType Type;
        /// <summary>
        /// 版本
        /// </summary>
        public const byte Version = 0;
        /// <summary>
        /// 该交易所具备的额外特性
        /// </summary>
        public TransactionAttribute[] Attributes;
        /// <summary>
        /// 输入列表
        /// </summary>
        public TransactionInput[] Inputs;
        /// <summary>
        /// 输出列表
        /// </summary>
        public TransactionOutput[] Outputs;
        /// <summary>
        /// 用于验证该交易的脚本列表
        /// </summary>
        public Script[] Scripts { get; set; }

        [NonSerialized]
        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(this.GetHashData().Sha256().Sha256());
                }
                return _hash;
            }
        }

        /// <summary>
        /// 清单类型
        /// </summary>
        InventoryType IInventory.InventoryType => InventoryType.TX;

        [NonSerialized]
        private IReadOnlyDictionary<TransactionInput, TransactionOutput> _references;
        /// <summary>
        /// 每一个交易输入所引用的交易输出
        /// </summary>
        public IReadOnlyDictionary<TransactionInput, TransactionOutput> References
        {
            get
            {
                if (_references == null)
                {
                    Dictionary<TransactionInput, TransactionOutput> dictionary = new Dictionary<TransactionInput, TransactionOutput>();
                    foreach (var group in GetAllInputs().GroupBy(p => p.PrevHash))
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

        /// <summary>
        /// 系统费用
        /// </summary>
        public virtual Fixed8 SystemFee => Fixed8.Zero;

        /// <summary>
        /// 用指定的类型初始化Transaction对象
        /// </summary>
        /// <param name="type">交易类型</param>
        protected Transaction(TransactionType type)
        {
            this.Type = type;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader">数据来源</param>
        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((ISignable)this).DeserializeUnsigned(reader);
            Scripts = reader.ReadSerializableArray<Script>();
            OnDeserialized();
        }

        /// <summary>
        /// 反序列化交易中的额外数据
        /// </summary>
        /// <param name="reader">数据来源</param>
        protected virtual void DeserializeExclusiveData(BinaryReader reader)
        {
        }

        /// <summary>
        /// 从指定的字节数组反序列化一笔交易
        /// </summary>
        /// <param name="value">字节数组</param>
        /// <param name="offset">偏移量，反序列化从该偏移量处开始</param>
        /// <returns>返回反序列化后的结果</returns>
        public static Transaction DeserializeFrom(byte[] value, int offset = 0)
        {
            using (MemoryStream ms = new MemoryStream(value, offset, value.Length - offset, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                return DeserializeFrom(reader);
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader">数据来源</param>
        /// <returns>返回反序列化后的结果</returns>
        internal static Transaction DeserializeFrom(BinaryReader reader)
        {
            TransactionType type = (TransactionType)reader.ReadByte();
            string typeName = string.Format("{0}.{1}", typeof(Transaction).Namespace, type);
            Transaction transaction = typeof(Transaction).Assembly.CreateInstance(typeName) as Transaction;
            if (transaction == null)
                throw new FormatException();
            transaction.DeserializeUnsignedWithoutType(reader);
            transaction.Scripts = reader.ReadSerializableArray<Script>();
            return transaction;
        }

        void ISignable.DeserializeUnsigned(BinaryReader reader)
        {
            if ((TransactionType)reader.ReadByte() != Type)
                throw new FormatException();
            DeserializeUnsignedWithoutType(reader);
        }

        private void DeserializeUnsignedWithoutType(BinaryReader reader)
        {
            if (reader.ReadByte() != Version)
                throw new FormatException();
            DeserializeExclusiveData(reader);
            Attributes = reader.ReadSerializableArray<TransactionAttribute>();
            if (Attributes.Select(p => p.Usage).Distinct().Count() != Attributes.Length)
                throw new FormatException();
            Inputs = reader.ReadSerializableArray<TransactionInput>();
            TransactionInput[] inputs = GetAllInputs().ToArray();
            for (int i = 1; i < inputs.Length; i++)
                for (int j = 0; j < i; j++)
                    if (inputs[i].PrevHash == inputs[j].PrevHash && inputs[i].PrevIndex == inputs[j].PrevIndex)
                        throw new FormatException();
            Outputs = reader.ReadSerializableArray<TransactionOutput>();
            if (Outputs.Length > ushort.MaxValue + 1)
                throw new FormatException();
        }

        public bool Equals(Transaction other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Transaction);
        }

        /// <summary>
        /// 获取交易的所有输入
        /// </summary>
        /// <returns>返回交易的所有输入</returns>
        public virtual IEnumerable<TransactionInput> GetAllInputs()
        {
            return Inputs;
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        /// <summary>
        /// 获取需要校验的脚本散列值
        /// </summary>
        /// <returns>返回需要校验的脚本散列值</returns>
        public virtual UInt160[] GetScriptHashesForVerifying()
        {
            if (References == null) throw new InvalidOperationException();
            HashSet<UInt160> hashes = new HashSet<UInt160>(Inputs.Select(p => References[p].ScriptHash));
            foreach (var group in Outputs.GroupBy(p => p.AssetId))
            {
                RegisterTransaction tx = Blockchain.Default.GetTransaction(group.Key) as RegisterTransaction;
                if (tx == null) throw new InvalidOperationException();
                if (tx.AssetType.HasFlag(AssetType.DutyFlag))
                {
                    hashes.UnionWith(group.Select(p => p.ScriptHash));
                }
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        /// <summary>
        /// 获取交易后各资产的变化量
        /// </summary>
        /// <returns>返回交易后各资产的变化量</returns>
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

        /// <summary>
        /// 通知子类反序列化完毕
        /// </summary>
        protected virtual void OnDeserialized()
        {
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((ISignable)this).SerializeUnsigned(writer);
            writer.Write(Scripts);
        }

        /// <summary>
        /// 序列化交易中的额外数据
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        protected virtual void SerializeExclusiveData(BinaryWriter writer)
        {
        }

        void ISignable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write(Version);
            SerializeExclusiveData(writer);
            writer.Write(Attributes);
            writer.Write(Inputs);
            writer.Write(Outputs);
        }

        /// <summary>
        /// 变成json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public virtual JObject ToJson()
        {
            JObject json = new JObject();
            json["txid"] = Hash.ToString();
            json["type"] = Type;
            json["version"] = Version;
            json["attributes"] = Attributes.Select(p => p.ToJson()).ToArray();
            json["vin"] = Inputs.Select(p => p.ToJson()).ToArray();
            json["vout"] = Outputs.Select((p, i) => p.ToJson((ushort)i)).ToArray();
            json["scripts"] = Scripts.Select(p => p.ToJson()).ToArray();
            return json;
        }

        /// <summary>
        /// 验证交易
        /// </summary>
        /// <returns>返回验证的结果</returns>
        public virtual bool Verify()
        {
            if (Blockchain.Default.ContainsTransaction(Hash)) return true;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes) || !Blockchain.Default.Ability.HasFlag(BlockchainAbility.TransactionIndexes))
                return false;
            if (Blockchain.Default.IsDoubleSpend(this))
                return false;
            foreach (var group in Outputs.GroupBy(p => p.AssetId))
            {
                RegisterTransaction asset = Blockchain.Default.GetTransaction(group.Key) as RegisterTransaction;
                if (asset == null) return false;
                foreach (TransactionOutput output in group)
                    if (output.Value.GetData() % (long)Math.Pow(10, 8 - asset.Precision) != 0)
                        return false;
            }
            TransactionResult[] results = GetTransactionResults()?.ToArray();
            if (results == null) return false;
            TransactionResult[] results_destroy = results.Where(p => p.Amount > Fixed8.Zero).ToArray();
            if (results_destroy.Length > 1) return false;
            if (results_destroy.Length == 1 && results_destroy[0].AssetId != Blockchain.AntCoin.Hash)
                return false;
            if (SystemFee > Fixed8.Zero && (results_destroy.Length == 0 || results_destroy[0].Amount < SystemFee))
                return false;
            TransactionResult[] results_issue = results.Where(p => p.Amount < Fixed8.Zero).ToArray();
            switch (Type)
            {
                case TransactionType.MinerTransaction:
                case TransactionType.ClaimTransaction:
                    if (results_issue.Any(p => p.AssetId != Blockchain.AntCoin.Hash))
                        return false;
                    break;
                case TransactionType.IssueTransaction:
                    if (results_issue.Any(p => p.AssetId == Blockchain.AntCoin.Hash))
                        return false;
                    break;
                default:
                    if (results_issue.Length > 0)
                        return false;
                    break;
            }
            TransactionAttribute script = Attributes.FirstOrDefault(p => p.Usage == TransactionAttributeUsage.Script);
            if (script != null)
            {
                ScriptEngine engine = new ScriptEngine(new Script
                {
                    StackScript = new byte[0],
                    RedeemScript = script.Data
                }, this, InterfaceEngine.Default);
                if (!engine.Execute()) return false;
            }
            return this.VerifySignature();
        }
    }
}
