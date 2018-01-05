using Neo.Core;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.IO;

namespace Neo.Network.Payloads
{
    public class ConsensusPayload : IInventory
    {
        public uint Version;
        public UInt256 PrevHash;
        public uint BlockIndex;
        public ushort ValidatorIndex;
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
            ValidatorIndex = reader.ReadUInt16();
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
            ECPoint[] validators = Blockchain.Default.GetValidators();
            if (validators.Length <= ValidatorIndex)
                throw new InvalidOperationException();
            return new[] { Contract.CreateSignatureRedeemScript(validators[ValidatorIndex]).ToScriptHash() };
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
            writer.Write(ValidatorIndex);
            writer.Write(Timestamp);
            writer.WriteVarBytes(Data);
        }

        public bool Verify()
        {
            if (Blockchain.Default == null) return false;
            if (BlockIndex <= Blockchain.Default.Height)
                return false;
            return this.VerifyScripts();
        }
    }
}
