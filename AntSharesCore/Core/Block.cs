using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.IO.Json;
using AntShares.Network;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    /// <summary>
    /// 区块或区块头
    /// </summary>
    public class Block : Inventory, IEquatable<Block>, ISignable
    {
        /// <summary>
        /// 区块版本
        /// </summary>
        public uint Version;
        /// <summary>
        /// 前一个区块的散列值
        /// </summary>
        public UInt256 PrevBlock;
        /// <summary>
        /// 该区块中所有交易的Merkle树的根
        /// </summary>
        public UInt256 MerkleRoot;
        /// <summary>
        /// 时间戳
        /// </summary>
        public uint Timestamp;
        /// <summary>
        /// 区块高度
        /// </summary>
        public uint Height;
        /// <summary>
        /// 随机数
        /// </summary>
        public ulong Nonce;
        /// <summary>
        /// 下一个区块的记账合约的散列值
        /// </summary>
        public UInt160 NextMiner;
        /// <summary>
        /// 用于验证该区块的脚本
        /// </summary>
        public Script Script;
        /// <summary>
        /// 交易列表，当列表中交易的数量为0时，该Block对象表示一个区块头
        /// </summary>
        public Transaction[] Transactions;

        private Block _header = null;
        /// <summary>
        /// 该区块的区块头
        /// </summary>
        public Block Header
        {
            get
            {
                if (IsHeader) return this;
                if (_header == null)
                {
                    _header = new Block
                    {
                        PrevBlock = PrevBlock,
                        MerkleRoot = MerkleRoot,
                        Timestamp = Timestamp,
                        Height = Height,
                        Nonce = Nonce,
                        NextMiner = NextMiner,
                        Script = Script,
                        Transactions = new Transaction[0]
                    };
                }
                return _header;
            }
        }

        /// <summary>
        /// 资产清单的类型
        /// </summary>
        public override InventoryType InventoryType => InventoryType.Block;

        Script[] ISignable.Scripts
        {
            get
            {
                return new[] { Script };
            }
            set
            {
                if (value.Length != 1) throw new ArgumentException();
                Script = value[0];
            }
        }

        /// <summary>
        /// 返回当前Block对象是否为区块头
        /// </summary>
        public bool IsHeader => Transactions.Length == 0;

        public static Fixed8 CalculateNetFee(IEnumerable<Transaction> transactions)
        {
            Transaction[] ts = transactions.Where(p => p.Type != TransactionType.MinerTransaction && p.Type != TransactionType.ClaimTransaction).ToArray();
            Fixed8 amount_in = ts.SelectMany(p => p.References.Values.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
            Fixed8 amount_out = ts.SelectMany(p => p.Outputs.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
            Fixed8 amount_sysfee = ts.Sum(p => p.SystemFee);
            return amount_in - amount_out - amount_sysfee;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader">数据来源</param>
        public override void Deserialize(BinaryReader reader)
        {
            ((ISignable)this).DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1) throw new FormatException();
            Script = reader.ReadSerializable<Script>();
            Transactions = new Transaction[reader.ReadVarInt(0x10000000)];
            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i] = Transaction.DeserializeFrom(reader);
            }
            if (Transactions.Length > 0)
            {
                if (Transactions[0].Type != TransactionType.MinerTransaction || Transactions.Skip(1).Any(p => p.Type == TransactionType.MinerTransaction))
                    throw new FormatException();
                if (MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray()) != MerkleRoot)
                    throw new FormatException();
            }
        }

        void ISignable.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            PrevBlock = reader.ReadSerializable<UInt256>();
            MerkleRoot = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            Nonce = reader.ReadUInt64();
            NextMiner = reader.ReadSerializable<UInt160>();
            Transactions = new Transaction[0];
        }

        /// <summary>
        /// 比较当前区块与指定区块是否相等
        /// </summary>
        /// <param name="other">要比较的区块</param>
        /// <returns>返回对象是否相等</returns>
        public bool Equals(Block other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return Hash.Equals(other.Hash);
        }

        /// <summary>
        /// 比较当前区块与指定区块是否相等
        /// </summary>
        /// <param name="obj">要比较的区块</param>
        /// <returns>返回对象是否相等</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as Block);
        }

        public static Block FromTrimmedData(byte[] data, int index, Func<UInt256, Transaction> txSelector = null)
        {
            Block block = new Block();
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                ((ISignable)block).DeserializeUnsigned(reader);
                reader.ReadByte(); block.Script = reader.ReadSerializable<Script>();
                if (txSelector == null)
                {
                    block.Transactions = new Transaction[0];
                }
                else
                {
                    block.Transactions = new Transaction[reader.ReadVarInt(0x10000000)];
                    for (int i = 0; i < block.Transactions.Length; i++)
                    {
                        block.Transactions[i] = txSelector(reader.ReadSerializable<UInt256>());
                    }
                }
            }
            return block;
        }

        /// <summary>
        /// 获得区块的HashCode
        /// </summary>
        /// <returns>返回区块的HashCode</returns>
        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        /// <summary>
        /// 获得区块中要计算Hash的数据
        /// </summary>
        /// <returns>返回区块中要计算Hash的数据</returns>
        protected override byte[] GetHashData()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                ((ISignable)this).SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            if (PrevBlock == UInt256.Zero)
                return new UInt160[] { new byte[0].ToScriptHash() };
            Block prev_header = Blockchain.Default.GetHeader(PrevBlock);
            if (prev_header == null) throw new InvalidOperationException();
            return new UInt160[] { prev_header.NextMiner };
        }

        /// <summary>
        /// 根据区块中所有交易的Hash生成MerkleRoot
        /// </summary>
        public void RebuildMerkleRoot()
        {
            MerkleRoot = MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray());
        }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="writer">存放序列化后的数据</param>
        public override void Serialize(BinaryWriter writer)
        {
            ((ISignable)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Script);
            writer.Write(Transactions);
        }

        void ISignable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevBlock);
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Height);
            writer.Write(Nonce);
            writer.Write(NextMiner);
        }

        /// <summary>
        /// 变成json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["version"] = Version;
            json["previousblockhash"] = PrevBlock.ToString();
            json["merkleroot"] = MerkleRoot.ToString();
            json["time"] = Timestamp;
            json["height"] = Height;
            json["nonce"] = Nonce.ToString("x16");
            json["nextminer"] = Wallet.ToAddress(NextMiner);
            json["script"] = Script.ToJson();
            json["tx"] = Transactions.Select(p => p.ToJson()).ToArray();
            return json;
        }

        /// <summary>
        /// 把区块对象变为只包含区块头和交易Hash的字节数组，去除交易数据
        /// </summary>
        /// <returns>返回只包含区块头和交易Hash的字节数组</returns>
        public byte[] Trim()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                ((ISignable)this).SerializeUnsigned(writer);
                writer.Write((byte)1); writer.Write(Script);
                writer.Write(Transactions.Select(p => p.Hash).ToArray());
                writer.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 验证该区块头是否合法
        /// </summary>
        /// <returns>返回该区块头的合法性，返回true即为合法，否则，非法。</returns>
        public override bool Verify()
        {
            return Verify(false);
        }

        /// <summary>
        /// 验证该区块头是否合法
        /// </summary>
        /// <param name="completely">是否同时验证区块中的每一笔交易</param>
        /// <returns>返回该区块头的合法性，返回true即为合法，否则，非法。</returns>
        public bool Verify(bool completely)
        {
            if (Hash == Blockchain.GenesisBlock.Hash) return true;
            if (Blockchain.Default.ContainsBlock(Hash)) return true;
            if (completely && IsHeader) return false;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.TransactionIndexes) || !Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return false;
            Block prev_header = Blockchain.Default.GetHeader(PrevBlock);
            if (prev_header == null) return false;
            if (prev_header.Height + 1 != Height) return false;
            if (prev_header.Timestamp >= Timestamp) return false;
            if (!this.VerifySignature()) return false;
            if (completely)
            {
                if (NextMiner != Blockchain.GetMinerAddress(Blockchain.Default.GetMiners(Transactions).ToArray()))
                    return false;
                foreach (Transaction tx in Transactions)
                    if (!tx.Verify()) return false;
                Transaction tx_gen = Transactions.FirstOrDefault(p => p.Type == TransactionType.MinerTransaction);
                if (tx_gen?.Outputs.Sum(p => p.Value) != CalculateNetFee(Transactions)) return false;
            }
            return true;
        }
    }
}
