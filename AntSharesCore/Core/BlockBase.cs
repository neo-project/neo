using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.IO.Json;
using AntShares.Wallets;
using System;
using System.IO;

namespace AntShares.Core
{
    public abstract class BlockBase : ISignable
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

        public virtual int Size => sizeof(uint) + PrevBlock.Size + MerkleRoot.Size + sizeof(uint) + sizeof(uint) + sizeof(ulong) + NextMiner.Size + 1 + Script.Size;

        public virtual void Deserialize(BinaryReader reader)
        {
            ((ISignable)this).DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1) throw new FormatException();
            Script = reader.ReadSerializable<Script>();
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
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            if (PrevBlock == UInt256.Zero)
                return new[] { Script.RedeemScript.ToScriptHash() };
            Header prev_header = Blockchain.Default.GetHeader(PrevBlock);
            if (prev_header == null) throw new InvalidOperationException();
            return new UInt160[] { prev_header.NextMiner };
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            ((ISignable)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Script);
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

        public virtual JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["previousblockhash"] = PrevBlock.ToString();
            json["merkleroot"] = MerkleRoot.ToString();
            json["time"] = Timestamp;
            json["height"] = Height;
            json["nonce"] = Nonce.ToString("x16");
            json["nextminer"] = Wallet.ToAddress(NextMiner);
            json["script"] = Script.ToJson();
            return json;
        }

        public bool Verify()
        {
            if (Hash == Blockchain.GenesisBlock.Hash) return true;
            if (Blockchain.Default.ContainsBlock(Hash)) return true;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.TransactionIndexes) || !Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return false;
            Header prev_header = Blockchain.Default.GetHeader(PrevBlock);
            if (prev_header == null) return false;
            if (prev_header.Height + 1 != Height) return false;
            if (prev_header.Timestamp >= Timestamp) return false;
            if (!this.VerifySignature()) return false;
            return true;
        }
    }
}
