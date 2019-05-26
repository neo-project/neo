using Neo.Consensus;
using Neo.Cryptography;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract.Native;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class ConsensusPayload : IInventory
    {
        public uint Version;
        public UInt256 PrevHash;
        public uint BlockIndex;
        public ushort ValidatorIndex;
        public byte[] Data;
        public byte[] Signature;

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
                    _hash = new UInt256(Crypto.Default.Hash256(this.GetHashData()));
                }
                return _hash;
            }
        }

        InventoryType IInventory.InventoryType => InventoryType.Consensus;

        public int Size =>
            sizeof(uint) +      //Version
            PrevHash.Size +     //PrevHash
            sizeof(uint) +      //BlockIndex
            sizeof(ushort) +    //ValidatorIndex
            sizeof(uint) +      //Timestamp
            Data.GetVarSize() + //Data
            Signature.Length;   //Signature

        public T GetDeserializedMessage<T>() where T : ConsensusMessage
        {
            return (T)ConsensusMessage;
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            Signature = reader.ReadBytes(64);
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            BlockIndex = reader.ReadUInt32();
            ValidatorIndex = reader.ReadUInt16();
            Data = reader.ReadVarBytes();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(Signature);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(BlockIndex);
            writer.Write(ValidatorIndex);
            writer.WriteVarBytes(Data);
        }

        public bool Verify(Snapshot snapshot)
        {
            if (BlockIndex <= snapshot.Height) return false;
            ECPoint[] validators = NativeContract.NEO.GetNextBlockValidators(snapshot);
            if (validators.Length <= ValidatorIndex) return false;
            return Crypto.Default.VerifySignature(this.GetHashData(), Signature, validators[ValidatorIndex].EncodePoint(false));
        }
    }
}
