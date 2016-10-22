using AntShares.Core;
using AntShares.Core.Scripts;
using AntShares.Cryptography;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.Wallets;
using System;
using System.IO;

namespace AntShares.Network.Payloads
{
    public class ConsensusPayload : IInventory
    {
        public uint Version;
        public UInt256 PrevHash;
        public uint Height;
        public ushort MinerIndex;
        public uint Timestamp;
        public byte[] Data;
        public Script Script;

        private UInt256 _hash = null;
        UInt256 IInventory.Hash
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

        InventoryType IInventory.InventoryType => InventoryType.Consensus;

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

        public int Size => sizeof(uint) + PrevHash.Size + sizeof(uint) + sizeof(ushort) + sizeof(uint) + Data.Length.GetVarSize() + Data.Length + 1 + Script.Size;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((ISignable)this).DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1) throw new FormatException();
            Script = reader.ReadSerializable<Script>();
        }

        void ISignable.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            Height = reader.ReadUInt32();
            MinerIndex = reader.ReadUInt16();
            Timestamp = reader.ReadUInt32();
            Data = reader.ReadVarBytes();
        }

        UInt160[] ISignable.GetScriptHashesForVerifying()
        {
            if (Blockchain.Default == null)
                throw new InvalidOperationException();
            if (PrevHash != Blockchain.Default.CurrentBlockHash)
                throw new InvalidOperationException();
            ECPoint[] miners = Blockchain.Default.GetMiners();
            if (miners.Length <= MinerIndex)
                throw new InvalidOperationException();
            return new[] { Contract.CreateSignatureContract(miners[MinerIndex]).ScriptHash };
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((ISignable)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Script);
        }

        void ISignable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(Height);
            writer.Write(MinerIndex);
            writer.Write(Timestamp);
            writer.WriteVarBytes(Data);
        }

        public bool Verify()
        {
            if (Blockchain.Default == null) return false;
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.TransactionIndexes) || !Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return false;
            if (PrevHash != Blockchain.Default.CurrentBlockHash)
                return false;
            if (Height != Blockchain.Default.Height + 1)
                return false;
            return this.VerifySignature();
        }
    }
}
