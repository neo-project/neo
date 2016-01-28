using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.IO.Json;
using AntShares.Network;
using AntShares.Wallets;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class Block : Inventory, IEquatable<Block>, ISignable
    {
        public const uint Version = 0;
        public UInt256 PrevBlock;
        public UInt256 MerkleRoot;
        public uint Timestamp;
        public uint Height;
        public ulong Nonce;
        public UInt160 NextMiner;
        public Script Script;
        public Transaction[] Transactions;

        [NonSerialized]
        private Block _header = null;
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

        public bool IsHeader => Transactions.Length == 0;

        public override void Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Version)
                throw new FormatException();
            PrevBlock = reader.ReadSerializable<UInt256>();
            MerkleRoot = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            Nonce = reader.ReadUInt64();
            NextMiner = reader.ReadSerializable<UInt160>();
            if (reader.ReadByte() != 1) throw new FormatException();
            Script = reader.ReadSerializable<Script>();
            Transactions = new Transaction[reader.ReadVarInt()];
            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i] = Transaction.DeserializeFrom(reader);
            }
            if (Transactions.Length > 0)
            {
                if (Transactions[0].Type != TransactionType.GenerationTransaction || Transactions.Skip(1).Any(p => p.Type == TransactionType.GenerationTransaction))
                    throw new FormatException();
                if (MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray()) != MerkleRoot)
                    throw new FormatException();
            }
        }

        void ISignable.DeserializeUnsigned(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Version)
                throw new FormatException();
            PrevBlock = reader.ReadSerializable<UInt256>();
            MerkleRoot = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt32();
            Height = reader.ReadUInt32();
            Nonce = reader.ReadUInt64();
            NextMiner = reader.ReadSerializable<UInt160>();
            Transactions = new Transaction[0];
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

        public static Block FromTrimmedData(byte[] data, int index, Func<UInt256, Transaction> txSelector = null)
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
                block.Height = reader.ReadUInt32();
                block.Nonce = reader.ReadUInt64();
                block.NextMiner = reader.ReadSerializable<UInt160>();
                reader.ReadByte(); block.Script = reader.ReadSerializable<Script>();
                if (txSelector == null)
                {
                    block.Transactions = new Transaction[0];
                }
                else
                {
                    block.Transactions = new Transaction[reader.ReadVarInt()];
                    for (int i = 0; i < block.Transactions.Length; i++)
                    {
                        block.Transactions[i] = txSelector(reader.ReadSerializable<UInt256>());
                    }
                }
            }
            return block;
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        protected override byte[] GetHashData()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(Version);
                writer.Write(PrevBlock);
                writer.Write(MerkleRoot);
                writer.Write(Timestamp);
                writer.Write(Height);
                writer.Write(Nonce);
                writer.Write(NextMiner);
                return ms.ToArray();
            }
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            if (PrevBlock == UInt256.Zero)
                return new UInt160[] { NextMiner };
            Block prev_header = Blockchain.Default.GetHeader(PrevBlock);
            if (prev_header == null) throw new InvalidOperationException();
            return new UInt160[] { prev_header.NextMiner };
        }

        public void RebuildMerkleRoot()
        {
            MerkleRoot = MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray());
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevBlock);
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Height);
            writer.Write(Nonce);
            writer.Write(NextMiner);
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

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["version"] = Version;
            json["previousblockhash"] = PrevBlock.ToString();
            json["merkleroot"] = MerkleRoot.ToString();
            json["time"] = Timestamp;
            json["height"] = Height;
            json["nonce"] = Nonce;
            json["nextminer"] = Wallet.ToAddress(NextMiner);
            json["script"] = Script.ToJson();
            json["tx"] = Transactions.Select(p => p.ToJson()).ToArray();
            return json;
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
                writer.Write(Height);
                writer.Write(Nonce);
                writer.Write(NextMiner);
                writer.Write((byte)1); writer.Write(Script);
                writer.Write(Transactions.Select(p => p.Hash).ToArray());
                writer.Flush();
                return ms.ToArray();
            }
        }

        public override bool Verify()
        {
            return Verify(false);
        }

        public bool Verify(bool completely)
        {
            if (Hash == Blockchain.GenesisBlock.Hash) return true;
            if (Blockchain.Default.ContainsBlock(Hash)) return true;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.TransactionIndexes) || !Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return false;
            Block prev_header = Blockchain.Default.GetHeader(PrevBlock);
            if (prev_header == null) return false;
            if (prev_header.Height + 1 != Height) return false;
            if (prev_header.Timestamp >= Timestamp) return false;
            if (!this.VerifySignature()) return false;
            if (NextMiner != Blockchain.GetMinerAddress(Blockchain.Default.GetMiners(Transactions).ToArray()))
                return false;
            if (completely)
            {
                if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.Statistics))
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
                Fixed8 gen = Fixed8.Zero;
                if (Height % Blockchain.MintingInterval == 0 && antshares.Length > 0)
                {
                    gen = Fixed8.FromDecimal((Blockchain.AntCoin.Amount - (quantity - amount_sysfee)).ToDecimal() * Blockchain.GenerationFactor);
                }
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
