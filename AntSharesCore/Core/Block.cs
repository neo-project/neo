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
    public class Block : Inventory, IEquatable<Block>, ISignable
    {
        public uint Version;
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

        public static Fixed8 CalculateNetFee(IEnumerable<Transaction> transactions)
        {
            Transaction[] ts = transactions.Where(p => p.Type != TransactionType.MinerTransaction && p.Type != TransactionType.ClaimTransaction).ToArray();
            Fixed8 amount_in = ts.SelectMany(p => p.References.Values.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
            Fixed8 amount_out = ts.SelectMany(p => p.Outputs.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
            Fixed8 amount_sysfee = ts.Sum(p => p.SystemFee);
            return amount_in - amount_out - amount_sysfee;
        }

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

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

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

        public void RebuildMerkleRoot()
        {
            MerkleRoot = MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray());
        }

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
                foreach (Transaction tx in Transactions)
                    if (!tx.Verify()) return false;
                Transaction tx_gen = Transactions.FirstOrDefault(p => p.Type == TransactionType.MinerTransaction);
                if (tx_gen?.Outputs.Sum(p => p.Value) != CalculateNetFee(Transactions)) return false;
            }
            return true;
        }
    }
}
