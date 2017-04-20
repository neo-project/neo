using AntShares.Core;
using AntShares.Cryptography;
using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.VM;
using AntShares.Wallets;
using System;
using System.IO;

namespace AntShares.Network.Payloads
{
    public class ConsensusPayload : IInventory
    {
        public uint Version;
        public UInt256 PrevHash;
        public uint BlockIndex;
        public ushort MinerIndex;
        public uint Timestamp;
        public byte[] Data;
        public Witness Script;

        private UInt256 _hash = null;
        UInt256 IInventory.Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Default.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }

        InventoryType IInventory.InventoryType => InventoryType.Consensus;

        Witness[] IVerifiable.Scripts
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

        public int Size => sizeof(uint) + PrevHash.Size + sizeof(uint) + sizeof(ushort) + sizeof(uint) + Data.GetVarSize() + 1 + Script.Size;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1) throw new FormatException();
            Script = reader.ReadSerializable<Witness>();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            BlockIndex = reader.ReadUInt32();
            MinerIndex = reader.ReadUInt16();
            Timestamp = reader.ReadUInt32();
            Data = reader.ReadVarBytes();
        }

        byte[] IScriptContainer.GetMessage()
        {
            return this.GetHashData();
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying()
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
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Script);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(BlockIndex);
            writer.Write(MinerIndex);
            writer.Write(Timestamp);
            writer.WriteVarBytes(Data);
        }

        byte[] IInteropInterface.ToArray()
        {
            return this.ToArray();
        }

        public bool Verify()
        {
            if (Blockchain.Default == null) return false;
            if (PrevHash != Blockchain.Default.CurrentBlockHash)
                return false;
            if (BlockIndex != Blockchain.Default.Height + 1)
                return false;
            return this.VerifyScripts();
        }
    }
}
