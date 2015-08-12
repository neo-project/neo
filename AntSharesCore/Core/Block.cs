using AntShares.Cryptography;
using AntShares.IO;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class Block : IEquatable<Block>, ISignable
    {
        public const uint Version = 0;
        public UInt256 PrevBlock;
        public UInt256 MerkleRoot;
        public uint Timestamp;
        public const uint Bits = 0;
        public ulong Nonce;
        public UInt160 NextMiner;
        public byte[] Script;
        public Transaction[] Transactions;

        public UInt256 Hash
        {
            get
            {
                return Header.Hash;
            }
        }

        private BlockHeader _header = null;
        public BlockHeader Header
        {
            get
            {
                if (_header == null)
                {
                    _header = new BlockHeader
                    {
                        PrevBlock = PrevBlock,
                        MerkleRoot = MerkleRoot,
                        Timestamp = Timestamp,
                        Nonce = Nonce,
                        NextMiner = NextMiner,
                        Script = Script,
                        TransactionCount = Transactions.Length
                    };
                }
                return _header;
            }
        }

        byte[][] ISignable.Scripts
        {
            get
            {
                return new byte[][] { Script };
            }
            set
            {
                if (value.Length != 1)
                    throw new ArgumentException();
                Script = value[0];
            }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Version)
                throw new FormatException();
            this.PrevBlock = reader.ReadSerializable<UInt256>();
            this.MerkleRoot = reader.ReadSerializable<UInt256>();
            this.Timestamp = reader.ReadUInt32();
            if (reader.ReadUInt32() != Bits)
                throw new FormatException();
            this.Nonce = reader.ReadUInt64();
            this.NextMiner = reader.ReadSerializable<UInt160>();
            this.Script = reader.ReadBytes((int)reader.ReadVarInt());
            this.Transactions = new Transaction[reader.ReadVarInt()];
            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i] = Transaction.DeserializeFrom(reader);
            }
            if (MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray()) != MerkleRoot)
                throw new FormatException();
        }

        public bool Equals(Block other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (ReferenceEquals(null, other)) return false;
            return Hash.Equals(other.Hash);
        }

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
                if (reader.ReadUInt32() != Version)
                    throw new FormatException();
                block.PrevBlock = reader.ReadSerializable<UInt256>();
                block.MerkleRoot = reader.ReadSerializable<UInt256>();
                block.Timestamp = reader.ReadUInt32();
                if (reader.ReadUInt32() != Bits)
                    throw new FormatException();
                block.Nonce = reader.ReadUInt64();
                block.NextMiner = reader.ReadSerializable<UInt160>();
                block.Script = reader.ReadBytes((int)reader.ReadVarInt());
                block.Transactions = new Transaction[reader.ReadVarInt()];
                for (int i = 0; i < block.Transactions.Length; i++)
                {
                    block.Transactions[i] = txSelector(reader.ReadSerializable<UInt256>());
                }
                if (MerkleTree.ComputeRoot(block.Transactions.Select(p => p.Hash).ToArray()) != block.MerkleRoot)
                    throw new FormatException();
            }
            return block;
        }

        void ISignable.FromUnsignedArray(byte[] value)
        {
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                if (reader.ReadUInt32() != Version)
                    throw new FormatException();
                this.PrevBlock = reader.ReadSerializable<UInt256>();
                this.MerkleRoot = reader.ReadSerializable<UInt256>();
                this.Timestamp = reader.ReadUInt32();
                if (reader.ReadUInt32() != Bits)
                    throw new FormatException();
                this.Nonce = reader.ReadUInt64();
                this.NextMiner = reader.ReadSerializable<UInt160>();
                this.Transactions = new Transaction[reader.ReadVarInt()];
                for (int i = 0; i < Transactions.Length; i++)
                {
                    Transactions[i] = Transaction.DeserializeFrom(reader);
                }
                if (MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray()) != MerkleRoot)
                    throw new FormatException();
            }
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        byte[] ISignable.GetHashForSigning()
        {
            return ((ISignable)Header).GetHashForSigning();
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            return ((ISignable)Header).GetScriptHashesForVerifying();
        }

        public void RebuildMerkleRoot()
        {
            MerkleRoot = MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray());
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevBlock);
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Bits);
            writer.Write(Nonce);
            writer.Write(NextMiner);
            writer.WriteVarInt(Script.Length); writer.Write(Script);
            writer.Write(Transactions);
        }

        byte[] ISignable.ToUnsignedArray()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(Version);
                writer.Write(PrevBlock);
                writer.Write(MerkleRoot);
                writer.Write(Timestamp);
                writer.Write(Bits);
                writer.Write(Nonce);
                writer.Write(NextMiner);
                writer.Write(Transactions);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public byte[] Trim()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(Version);
                writer.Write(PrevBlock);
                writer.Write(MerkleRoot);
                writer.Write(Timestamp);
                writer.Write(Bits);
                writer.Write(Nonce);
                writer.Write(NextMiner);
                writer.WriteVarInt(Script.Length); writer.Write(Script);
                writer.Write(Transactions.Select(p => p.Hash).ToArray());
                writer.Flush();
                return ms.ToArray();
            }
        }

        public VerificationResult Verify(bool completely = false)
        {
            VerificationResult result = VerificationResult.OK;
            if (Hash == Blockchain.GenesisBlock.Hash) return VerificationResult.OK;
            if (Transactions.Count(p => p.Type == TransactionType.GenerationTransaction) != 1)
                return VerificationResult.IncorrectFormat;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.TransactionIndexes) || !Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return VerificationResult.Incapable;
            if (!Blockchain.Default.ContainsBlock(PrevBlock))
                return VerificationResult.LackOfInformation;
            result |= this.VerifySignature();
            //TODO: 此处排序可能将耗费大量内存，考虑是否采用其它机制
            Vote[] votes = Blockchain.Default.GetVotes(Transactions).OrderBy(p => p.Enrollments.Length).ToArray();
            int miner_count = (int)votes.WeightedFilter(0.25, 0.75, p => p.Count.GetData(), (p, w) => new
            {
                MinerCount = p.Enrollments.Length,
                Weight = w
            }).WeightedAverage(p => p.MinerCount, p => p.Weight);
            miner_count = Math.Max(miner_count, Blockchain.StandbyMiners.Length);
            Dictionary<ECCPublicKey, Fixed8> miners = new Dictionary<ECCPublicKey, Fixed8>();
            Dictionary<UInt256, ECCPublicKey> enrollments = Blockchain.Default.GetEnrollments(Transactions).ToDictionary(p => p.Hash, p => p.PublicKey);
            foreach (var vote in votes)
            {
                foreach (UInt256 hash in vote.Enrollments)
                {
                    if (!enrollments.ContainsKey(hash)) continue;
                    ECCPublicKey pubkey = enrollments[hash];
                    if (!miners.ContainsKey(pubkey))
                    {
                        miners.Add(pubkey, Fixed8.Zero);
                    }
                    miners[pubkey] += vote.Count;
                }
            }
            ECCPublicKey[] pubkeys = miners.OrderByDescending(p => p.Value).Select(p => p.Key).Concat(Blockchain.StandbyMiners).Take(miner_count).ToArray();
            if (NextMiner != Wallet.CreateRedeemScript(Blockchain.GetMinSignatureCount(miner_count), pubkeys).ToScriptHash())
                result |= VerificationResult.WrongMiner;
            if (completely)
            {
                if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.Statistics))
                {
                    result |= VerificationResult.Incapable;
                    return result;
                }
                foreach (Transaction tx in Transactions)
                    result |= tx.Verify();
                var antshares = Blockchain.Default.GetUnspentAntShares().GroupBy(p => p.ScriptHash, (k, g) => new
                {
                    ScriptHash = k,
                    Amount = g.Sum(p => p.Value)
                }).OrderBy(p => p.Amount).ThenBy(p => p.ScriptHash).ToArray();
                Transaction[] transactions = Transactions.Where(p => p.Type != TransactionType.GenerationTransaction).ToArray();
                Fixed8 amount_in = transactions.SelectMany(p => p.References.Values.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
                Fixed8 amount_out = transactions.SelectMany(p => p.Outputs.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
                Fixed8 amount_sysfee = transactions.Sum(p => p.SystemFee);
                Fixed8 amount_netfee = amount_in - amount_out - amount_sysfee;
                Fixed8 quantity = Blockchain.Default.GetQuantityIssued(Blockchain.AntCoin.Hash);
                Fixed8 gen = antshares.Length == 0 ? Fixed8.Zero : Fixed8.FromDecimal((Blockchain.AntCoin.Amount - (quantity - amount_sysfee)).ToDecimal() * 2.4297257e-7m);
                GenerationTransaction tx_gen = Transactions.OfType<GenerationTransaction>().First();
                if (tx_gen.Outputs.Sum(p => p.Value) != amount_netfee + gen)
                    result |= VerificationResult.Imbalanced;
                if (antshares.Length > 0)
                {
                    ulong n = Nonce % (ulong)antshares.Sum(p => p.Amount).value;
                    ulong line = 0;
                    int i = -1;
                    do
                    {
                        line += (ulong)antshares[++i].Amount.value;
                    } while (line <= n);
                    if (tx_gen.Outputs.Where(p => p.ScriptHash == antshares[i].ScriptHash).Sum(p => p.Value) < gen)
                        result |= VerificationResult.Imbalanced;
                }
            }
            return result;
        }
    }
}
