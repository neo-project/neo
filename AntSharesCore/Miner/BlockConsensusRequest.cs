using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.Network;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Miner
{
    public class BlockConsensusRequest : Inventory, ISignable
    {
        public UInt256 PrevHash;
        public ECPoint Miner;
        public byte[] IV;
        public Dictionary<ECPoint, byte[]> NoncePieces = new Dictionary<ECPoint, byte[]>();
        public UInt256 NonceHash;
        public UInt256[] TransactionHashes;
        public Script Script;

        public override InventoryType InventoryType
        {
            get
            {
                return InventoryType.ConsRequest;
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

        public override void Deserialize(BinaryReader reader)
        {
            ((ISignable)this).DeserializeUnsigned(reader);
            Script = reader.ReadSerializable<Script>();
        }

        void ISignable.DeserializeUnsigned(BinaryReader reader)
        {
            PrevHash = reader.ReadSerializable<UInt256>();
            Miner = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            IV = reader.ReadBytes(16);
            NoncePieces.Clear();
            int count = (int)reader.ReadVarInt();
            for (int i = 0; i < count; i++)
            {
                ECPoint key = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
                if (key == Miner) throw new FormatException();
                byte[] value = reader.ReadBytes((int)reader.ReadVarInt());
                NoncePieces.Add(key, value);
            }
            NonceHash = reader.ReadSerializable<UInt256>();
            TransactionHashes = reader.ReadSerializableArray<UInt256>();
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            return new UInt160[] { SignatureContract.Create(Miner).ScriptHash };
        }

        public override void Serialize(BinaryWriter writer)
        {
            ((ISignable)this).SerializeUnsigned(writer);
            writer.Write(Script);
        }

        void ISignable.SerializeUnsigned(BinaryWriter writer)
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
        }

        public override bool Verify()
        {
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.TransactionIndexes) || !Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return false;
            if (PrevHash != Blockchain.Default.CurrentBlockHash)
                return false;
            HashSet<ECPoint> miners = new HashSet<ECPoint>(Blockchain.Default.GetMiners());
            if (!miners.Contains(Miner)) return false;
            if (NoncePieces.Count != miners.Count - 1) return false;
            if (!NoncePieces.Keys.Concat(new[] { Miner }).OrderBy(p => p).SequenceEqual(miners.OrderBy(p => p)))
                return false;
            return this.VerifySignature();
        }
    }
}
