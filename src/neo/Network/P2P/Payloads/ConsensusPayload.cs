using Neo.Consensus;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Models;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class ConsensusPayload : IWitnessed
    {
        private uint version;
        private UInt256 prevHash;
        private uint blockIndex;
        private byte validatorIndex;
        private byte[] data;
        private Witness witness;

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

        private Lazy<UInt256> hash;
        public UInt256 Hash
        {
            get
            {
                hash ??= new Lazy<UInt256>(() => this.CalculateHash(ProtocolSettings.Default.Magic));
                return hash.Value;
            }
        }

        // InventoryType IWitnessed.InventoryType => InventoryType.Consensus;

        public int Size =>
            sizeof(uint) +      //Version
            UInt256.Length +     //PrevHash
            sizeof(uint) +      //BlockIndex
            sizeof(byte) +      //ValidatorIndex
            Data.GetVarSize() + //Data
            1 + Witness.Size;   //Witness

        private Lazy<Witness[]> witnesses;
        Witness[] IWitnessed.Witnesses
        {
            get
            {
                witnesses ??= new Lazy<Witness[]>(() => new[] { Witness });
                return witnesses.Value;
            }
        }

        public uint Version { get => version; set { version = value; hash = null; } }
        public UInt256 PrevHash { get => prevHash; set { prevHash = value; hash = null; } }
        public uint BlockIndex { get => blockIndex; set { blockIndex = value; hash = null; } }
        public byte ValidatorIndex { get => validatorIndex; set { validatorIndex = value; hash = null; } }
        public byte[] Data { get => data; set { data = value; hash = null; } }
        public Witness Witness { get => witness; set { witness = value; witnesses = null; } }

        public T GetDeserializedMessage<T>() where T : ConsensusMessage
        {
            return (T)ConsensusMessage;
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((IWitnessed)this).DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1) throw new FormatException();
            Witness = reader.ReadSerializable<Witness>();
        }

        void IWitnessed.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            BlockIndex = reader.ReadUInt32();
            ValidatorIndex = reader.ReadByte();
            if (ValidatorIndex >= ProtocolSettings.Default.ValidatorsCount)
                throw new FormatException();
            Data = reader.ReadVarBytes();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IWitnessed)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Witness);
        }

        void IWitnessed.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(BlockIndex);
            writer.Write(ValidatorIndex);
            writer.WriteVarBytes(Data);
        }

        // public bool Verify(StoreView snapshot)
        // {
        //     if (BlockIndex <= snapshot.Height)
        //         return false;
        //     return this.VerifyWitnesses(snapshot, 0_02000000);
        // }
    }
}
