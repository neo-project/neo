using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.Network;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class Block : Inventory, IEquatable<Block>
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

        [NonSerialized]
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

        public override InventoryType InventoryType
        {
            get
            {
                return InventoryType.Block;
            }
        }

        public override void Deserialize(BinaryReader reader)
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

        protected override UInt256 GetHash()
        {
            return Header.Hash;
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
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
            writer.Write(Bits);
            writer.Write(Nonce);
            writer.Write(NextMiner);
            writer.WriteVarInt(Script.Length); writer.Write(Script);
            writer.Write(Transactions);
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

        public override VerificationResult Verify()
        {
            return Verify(false);
        }

        public VerificationResult Verify(bool completely)
        {
            if (Hash == Blockchain.GenesisBlock.Hash) return VerificationResult.AlreadyInBlockchain;
            if (Transactions.Count(p => p.Type == TransactionType.GenerationTransaction) != 1)
                return VerificationResult.IncorrectFormat;
            if (Blockchain.Default.ContainsBlock(Hash)) return VerificationResult.AlreadyInBlockchain;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.TransactionIndexes) || !Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return VerificationResult.Incapable;
            VerificationResult result = Header.Verify();
            if (result != VerificationResult.OK) return result;
            Secp256r1Point[] pubkeys = Blockchain.Default.GetMiners(Transactions).ToArray();
            if (NextMiner != ScriptBuilder.CreateRedeemScript(Blockchain.GetMinSignatureCount(pubkeys.Length), pubkeys).ToScriptHash())
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
                Fixed8 gen = antshares.Length == 0 ? Fixed8.Zero : Fixed8.FromDecimal((Blockchain.AntCoin.Amount - (quantity - amount_sysfee)).ToDecimal() * Blockchain.GenerationFactor);
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
