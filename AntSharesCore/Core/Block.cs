using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class Block : ISignable
    {
        public const UInt32 Version = 0;
        public UInt256 PrevBlock;
        public UInt256 MerkleRoot;
        public UInt32 Timestamp;
        public const UInt32 Bits = 0;
        public UInt32 Nonce;
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
            this.Nonce = reader.ReadUInt32();
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
                this.Nonce = reader.ReadUInt32();
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

        public bool Verify()
        {
            //TODO: 验证合法性
        }
    }
}
