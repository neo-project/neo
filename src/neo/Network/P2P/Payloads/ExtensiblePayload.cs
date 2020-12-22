using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class ExtensiblePayload : IVerifiable
    {
        public string Receiver;
        public byte MessageType;
        public byte[] Data;
        public Witness Witness;

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

        public int Size =>
            Receiver.GetVarSize() + //Receiver
            sizeof(byte) +          //MessageType
            Data.GetVarSize() +     //Data
            1 + Witness.Size;       //Witness

        Witness[] IVerifiable.Witnesses
        {
            get
            {
                return new[] { Witness };
            }
            set
            {
                if (value.Length != 1) throw new ArgumentException();
                Witness = value[0];
            }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            if (reader.ReadByte() != 1) throw new FormatException();
            Witness = reader.ReadSerializable<Witness>();
        }

        void IVerifiable.DeserializeUnsigned(BinaryReader reader)
        {
            Receiver = reader.ReadVarString(32);
            MessageType = reader.ReadByte();
            Data = reader.ReadVarBytes(ushort.MaxValue);
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(StoreView snapshot)
        {
            return new[] { Witness.ScriptHash }; // This address should be checked by consumer
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Witness);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.WriteVarString(Receiver);
            writer.Write(MessageType);
            writer.WriteVarBytes(Data);
        }

        public bool Verify(StoreView snapshot)
        {
            if (!Blockchain.Singleton.IsExtensibleWitnessWhiteListed(Witness.ScriptHash)) return false;
            return this.VerifyWitnesses(snapshot, 0_02000000);
        }
    }
}
