using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class Block : ISignable
    {
        public const uint Version = 0;
        public UInt256 PrevBlock;
        public UInt256 MerkleRoot;
        public uint Timestamp;
        public const uint Bits = 0;
        public ulong Nonce;
        public UInt160 Miner;
        public byte[] Script;
        public Transaction[] Transactions;

        private UInt256 hash = null;

        public UInt256 Hash
        {
            get
            {
                if (hash == null)
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
                        writer.Write(Miner);
                        writer.WriteVarInt(Script.Length); writer.Write(Script);
                        writer.Flush();
                        hash = new UInt256(ms.ToArray().Sha256().Sha256());
                    }
                }
                return hash;
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
            this.Miner = reader.ReadSerializable<UInt160>();
            this.Script = reader.ReadBytes((int)reader.ReadVarInt());
            if (!this.VerifySignature())
                throw new FormatException();
            this.Transactions = new Transaction[reader.ReadVarInt()];
            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i] = Transaction.DeserializeFrom(reader);
            }
            if (MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray()) != MerkleRoot)
                throw new FormatException();
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
                this.Miner = reader.ReadSerializable<UInt160>();
                this.Transactions = new Transaction[reader.ReadVarInt()];
                for (int i = 0; i < Transactions.Length; i++)
                {
                    Transactions[i] = Transaction.DeserializeFrom(reader);
                }
                if (MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray()) != MerkleRoot)
                    throw new FormatException();
            }
        }

        byte[] ISignable.GetHashForSigning()
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
                writer.Write(Miner);
                writer.Flush();
                return ms.ToArray().Sha256();
            }
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            return new UInt160[] { Miner };
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
            writer.Write(Miner);
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
                writer.Write(Miner);
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
                writer.Write(Miner);
                writer.WriteVarInt(Script.Length); writer.Write(Script);
                writer.Write(Transactions.Select(p => p.Hash).ToArray());
                writer.Flush();
                return ms.ToArray();
            }
        }

        public bool Verify(bool completely = false)
        {
            if (Transactions.Count(p => p.Type == TransactionType.GenerationTransaction) != 1)
                return false;
            if (!Blockchain.Default.ContainsBlock(PrevBlock))
                return false;
            if (!this.VerifySignature()) return false;
            //TODO: 验证Miner的合法性
            if (completely)
            {
                if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.Statistics) || !Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                    return false;
                foreach (Transaction tx in Transactions)
                    if (!tx.Verify()) return false;
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
                    return false;
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
                        return false;
                }
            }
            return true;
        }
    }
}
