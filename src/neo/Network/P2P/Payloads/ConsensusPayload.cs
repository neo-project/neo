using Neo.Consensus;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class ConsensusPayload : IInventory
    {
        public uint Version;
        public UInt256 PrevHash;
        public uint BlockIndex;
        public byte ValidatorIndex;
        public byte[] Data;
        public Witness Witness;

        private ConsensusMessage _deserializedMessage = null;
        public ConsensusMessage ConsensusMessage
        {
            get
            {
                if (_deserializedMessage is null)
                    _deserializedMessage = ConsensusMessage.DeserializeFrom(Data);
                return _deserializedMessage;
            }
            internal set
            {
                if (!ReferenceEquals(_deserializedMessage, value))
                {
                    _deserializedMessage = value;
                    Data = value?.ToArray();
                }
            }
        }

        private UInt256 _hash = null;
        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = this.CalculateHash();
                }
                return _hash;
            }
        }

        InventoryType IInventory.InventoryType => InventoryType.Consensus;

        public int Size =>
            sizeof(uint) +      //Version
            PrevHash.Size +     //PrevHash
            sizeof(uint) +      //BlockIndex
            sizeof(byte) +      //ValidatorIndex
            Data.GetVarSize() + //Data
            1 + Witness.Size;   //Witness

        Witness[] IVerifiable.Witnesses
        {
            get
            {
                return new[] { Witness };
            }
        }

        public T GetDeserializedMessage<T>() where T : ConsensusMessage
        {
            return (T)ConsensusMessage;
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1) throw new FormatException();
            Witness = reader.ReadSerializable<Witness>();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            BlockIndex = reader.ReadUInt32();
            ValidatorIndex = reader.ReadByte();
            if (ValidatorIndex >= ProtocolSettings.Default.ValidatorsCount)
                throw new FormatException();
            Data = reader.ReadVarBytes();
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(StoreView snapshot)
        {
            ECPoint[] validators = NativeContract.NEO.GetNextBlockValidators(snapshot);
            if (validators.Length <= ValidatorIndex)
                throw new InvalidOperationException();
            return new[] { Contract.CreateSignatureRedeemScript(validators[ValidatorIndex]).ToScriptHash() };
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Witness);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(BlockIndex);
            writer.Write(ValidatorIndex);
            writer.WriteVarBytes(Data);
        }

        public bool Verify(StoreView snapshot)
        {
            if (BlockIndex <= snapshot.Height)
                return false;
            return this.VerifyWitnesses(snapshot, 0_02000000);
        }
    }
}
