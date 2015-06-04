using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class Block : ISerializable
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
                    hash = new UInt256(this.ToArray().Sha256().Sha256());
                }
                return hash;
            }
        }

        private byte[] CreateScript()
        {
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                return sb.Add(Script).Push(ScriptOp.OP_DUP).Push(ScriptOp.OP_HASH160).Push(Miner).Push(ScriptOp.OP_EQUALVERIFY).Push(ScriptOp.OP_EVAL).ToArray();
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
            if (!ScriptEngine.Execute(CreateScript(), GetHashForSigning()))
                throw new FormatException();
            this.Transactions = new Transaction[reader.ReadVarInt()];
            for (int i = 0; i < Transactions.Length; i++)
            {
                Transactions[i] = Transaction.DeserializeFrom(reader);
            }
            if (MerkleTree.ComputeRoot(Transactions.Select(p => p.Hash).ToArray()) != MerkleRoot)
                throw new FormatException();
        }

        private byte[] GetHashForSigning()
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
                return ms.ToArray();
            }
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
    }
}
