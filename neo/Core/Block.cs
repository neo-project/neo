using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    /// <summary>
    /// 区块或区块头
    /// </summary>
    public class Block : BlockBase, IInventory, IEquatable<Block>
    {
        /// <summary>
        /// 交易列表
        /// </summary>
        public Transaction[] Transactions;

        private Header _header = null;
        /// <summary>
        /// 该区块的区块头
        /// </summary>
        public Header Header
        {
            get
            {
                if (_header == null)
                {
                    _header = new Header
                    {
                        PrevHash = PrevHash,
                        MerkleRoot = MerkleRoot,
                        Timestamp = Timestamp,
                        Index = Index,
                        ConsensusData = ConsensusData,
                        NextConsensus = NextConsensus,
                        Script = Script
                    };
                }
                return _header;
            }
        }

        /// <summary>
        /// 资产清单的类型
        /// </summary>
        InventoryType IInventory.InventoryType => InventoryType.Block;

        public override int Size => base.Size + Transactions.GetVarSize();

        public static Fixed8 CalculateNetFee(IEnumerable<Transaction> transactions)
        {
            Transaction[] ts = transactions.Where(p => p.Type != TransactionType.MinerTransaction && p.Type != TransactionType.ClaimTransaction).ToArray();
            Fixed8 amount_in = ts.SelectMany(p => p.References.Values.Where(o => o.AssetId == Blockchain.UtilityToken.Hash)).Sum(p => p.Value);
            Fixed8 amount_out = ts.SelectMany(p => p.Outputs.Where(o => o.AssetId == Blockchain.UtilityToken.Hash)).Sum(p => p.Value);
            Fixed8 amount_sysfee = ts.Sum(p => p.SystemFee);
            return amount_in - amount_out - amount_sysfee;
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="reader">数据来源</param>
        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Transactions = new Transaction[reader.ReadVarInt(0x10000)];
            if (Transactions.Length == 0) throw new FormatException();
            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i] = Transaction.DeserializeFrom(reader);
            }
            if (MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray()) != MerkleRoot)
                throw new FormatException();
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

        public static Block FromTrimmedData(byte[] data, int index, Func<UInt256, Transaction> txSelector)
        {
            Block block = new Block();
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                ((IVerifiable)block).DeserializeUnsigned(reader);
                reader.ReadByte(); block.Script = reader.ReadSerializable<Witness>();
                block.Transactions = new Transaction[reader.ReadVarInt(0x10000000)];
                for (int i = 0; i < block.Transactions.Length; i++)
                {
                    block.Transactions[i] = txSelector(reader.ReadSerializable<UInt256>());
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
            base.Serialize(writer);
            writer.Write(Transactions);
        }

        /// <summary>
        /// 变成json对象
        /// </summary>
        /// <returns>返回json对象</returns>
        public override JObject ToJson()
        {
            JObject json = base.ToJson();
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
                ((IVerifiable)this).SerializeUnsigned(writer);
                writer.Write((byte)1); writer.Write(Script);
                writer.Write(Transactions.Select(p => p.Hash).ToArray());
                writer.Flush();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 验证该区块是否合法
        /// </summary>
        /// <param name="completely">是否同时验证区块中的每一笔交易</param>
        /// <returns>返回该区块的合法性，返回true即为合法，否则，非法。</returns>
        public bool Verify(bool completely)
        {
            if (!Verify()) return false;
            if (Transactions[0].Type != TransactionType.MinerTransaction || Transactions.Skip(1).Any(p => p.Type == TransactionType.MinerTransaction))
                return false;
            if (completely)
            {
                if (NextConsensus != Blockchain.GetConsensusAddress(Blockchain.Default.GetValidators(Transactions).ToArray()))
                    return false;
                foreach (Transaction tx in Transactions)
                    if (!tx.Verify(Transactions.Where(p => !p.Hash.Equals(tx.Hash)))) return false;
                Transaction tx_gen = Transactions.FirstOrDefault(p => p.Type == TransactionType.MinerTransaction);
                if (tx_gen?.Outputs.Sum(p => p.Value) != CalculateNetFee(Transactions)) return false;
            }
            return true;
        }
    }
}
