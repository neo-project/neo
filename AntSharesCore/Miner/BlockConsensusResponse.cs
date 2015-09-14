using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.IO;
using AntShares.Network;
using System;
using System.Collections.Generic;
using System.IO;

namespace AntShares.Miner
{
    public class BlockConsensusResponse : Inventory, ISignable
    {
        public UInt256 PrevHash;
        public Secp256r1Point Miner;
        public Dictionary<Secp256r1Point, byte[]> NoncePieces = new Dictionary<Secp256r1Point, byte[]>();
        public UInt256 MerkleRoot;
        public byte[] Script;

        public override InventoryType InventoryType => InventoryType.ConsResponse;

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
            ((ISignable)this).DeserializeUnsigned(reader);
            this.Script = reader.ReadBytes((int)reader.ReadVarInt());
        }

        void ISignable.DeserializeUnsigned(BinaryReader reader)
        {
            this.PrevHash = reader.ReadSerializable<UInt256>();
            this.Miner = Secp256r1Point.DeserializeFrom(reader);
            this.NoncePieces.Clear();
            int count = (int)reader.ReadVarInt();
            for (int i = 0; i < count; i++)
            {
                Secp256r1Point key = Secp256r1Point.DeserializeFrom(reader);
                byte[] value = reader.ReadBytes((int)reader.ReadVarInt());
                NoncePieces.Add(key, value);
            }
            this.MerkleRoot = reader.ReadSerializable<UInt256>();
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            return new UInt160[] { ScriptBuilder.CreateRedeemScript(1, Miner).ToScriptHash() };
        }

        public override void Serialize(BinaryWriter writer)
        {
            ((ISignable)this).SerializeUnsigned(writer);
            writer.WriteVarInt(Script.Length); writer.Write(Script);
        }

        void ISignable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(PrevHash);
            writer.Write(Miner);
            writer.WriteVarInt(NoncePieces.Count);
            foreach (var pair in NoncePieces)
            {
                writer.Write(pair.Key);
                writer.WriteVarInt(pair.Value.Length); writer.Write(pair.Value);
            }
            writer.Write(MerkleRoot);
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
            if (NoncePieces.Count > miners.Count)
                return VerificationResult.IncorrectFormat;
            return this.VerifySignature();
        }
    }
}
