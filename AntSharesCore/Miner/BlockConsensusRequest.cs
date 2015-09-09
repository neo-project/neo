using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Miner
{
    public class BlockConsensusRequest : Inventory, ISignable
    {
        public UInt256 PrevHash;
        public Secp256r1Point Miner;
        public byte[] IV;
        public Dictionary<Secp256r1Point, byte[]> NoncePieces = new Dictionary<Secp256r1Point, byte[]>();
        public UInt256 NonceHash;
        public UInt256[] TransactionHashes;
        public byte[] Script;

        public override InventoryType InventoryType
        {
            get
            {
                return InventoryType.ConsRequest;
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

        public override void Deserialize(BinaryReader reader)
        {
            this.PrevHash = reader.ReadSerializable<UInt256>();
            this.Miner = Secp256r1Point.DeserializeFrom(reader);
            this.IV = reader.ReadBytes(16);
            this.NoncePieces.Clear();
            int count = (int)reader.ReadVarInt();
            for (int i = 0; i < count; i++)
            {
                Secp256r1Point key = Secp256r1Point.DeserializeFrom(reader);
                if (key == Miner) throw new FormatException();
                byte[] value = reader.ReadBytes((int)reader.ReadVarInt());
                NoncePieces.Add(key, value);
            }
            this.NonceHash = reader.ReadSerializable<UInt256>();
            this.TransactionHashes = reader.ReadSerializableArray<UInt256>();
            this.Script = reader.ReadBytes((int)reader.ReadVarInt());
        }

        void ISignable.FromUnsignedArray(byte[] value)
        {
            using (MemoryStream ms = new MemoryStream(value, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                this.PrevHash = reader.ReadSerializable<UInt256>();
                this.Miner = Secp256r1Point.DeserializeFrom(reader);
                this.IV = reader.ReadBytes(16);
                this.NoncePieces.Clear();
                int count = (int)reader.ReadVarInt();
                for (int i = 0; i < count; i++)
                {
                    Secp256r1Point key = Secp256r1Point.DeserializeFrom(reader);
                    if (key == Miner) throw new FormatException();
                    value = reader.ReadBytes((int)reader.ReadVarInt());
                    NoncePieces.Add(key, value);
                }
                this.NonceHash = reader.ReadSerializable<UInt256>();
                this.TransactionHashes = reader.ReadSerializableArray<UInt256>();
            }
        }

        byte[] ISignable.GetHashForSigning()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(PrevHash);
                writer.Write(Miner);
                writer.Write(IV);
                writer.WriteVarInt(NoncePieces.Count);
                foreach (var pair in NoncePieces)
                {
                    writer.Write(pair.Key);
                    writer.WriteVarInt(pair.Value.Length); writer.Write(pair.Value);
                }
                writer.Write(NonceHash);
                writer.Write(TransactionHashes);
                writer.Flush();
                return ms.ToArray().Sha256();
            }
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            return new UInt160[] { ScriptBuilder.CreateRedeemScript(1, Miner).ToScriptHash() };
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(PrevHash);
            writer.Write(Miner);
            writer.Write(IV);
            writer.WriteVarInt(NoncePieces.Count);
            foreach (var pair in NoncePieces)
            {
                writer.Write(pair.Key);
                writer.WriteVarInt(pair.Value.Length); writer.Write(pair.Value);
            }
            writer.Write(NonceHash);
            writer.Write(TransactionHashes);
            writer.WriteVarInt(Script.Length); writer.Write(Script);
        }

        byte[] ISignable.ToUnsignedArray()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                writer.Write(PrevHash);
                writer.Write(Miner);
                writer.Write(IV);
                writer.WriteVarInt(NoncePieces.Count);
                foreach (var pair in NoncePieces)
                {
                    writer.Write(pair.Key);
                    writer.WriteVarInt(pair.Value.Length); writer.Write(pair.Value);
                }
                writer.Write(NonceHash);
                writer.Write(TransactionHashes);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public override VerificationResult Verify()
        {
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.TransactionIndexes) || !Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return VerificationResult.Incapable;
            if (!Blockchain.Default.ContainsBlock(PrevHash))
                return VerificationResult.LackOfInformation;
            if (PrevHash != Blockchain.Default.CurrentBlockHash)
                return VerificationResult.AlreadyInBlockchain;
            HashSet<Secp256r1Point> miners = new HashSet<Secp256r1Point>(Blockchain.Default.GetMiners());
            if (!miners.Contains(Miner))
                return VerificationResult.WrongMiner;
            if (NoncePieces.Count != miners.Count - 1)
                return VerificationResult.IncorrectFormat;
            if (!NoncePieces.Keys.Concat(new[] { Miner }).OrderBy(p => p).SequenceEqual(miners.OrderBy(p => p)))
                return VerificationResult.IncorrectFormat;
            return this.VerifySignature();
        }
    }
}
