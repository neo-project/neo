using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class ExtensiblePayload : IInventory
    {
        public string Category;
        public uint ValidBlockStart;
        public uint ValidBlockEnd;
        public UInt160 Sender;
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

        InventoryType IInventory.InventoryType => InventoryType.Extensible;

        public int Size =>
            Category.GetVarSize() + //Category
            sizeof(uint) +          //ValidBlockStart
            sizeof(uint) +          //ValidBlockEnd
            UInt160.Length +        //Sender
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
            Category = reader.ReadVarString(32);
            ValidBlockStart = reader.ReadUInt32();
            ValidBlockEnd = reader.ReadUInt32();
            if (ValidBlockStart >= ValidBlockEnd) throw new FormatException();
            Sender = reader.ReadSerializable<UInt160>();
            Data = reader.ReadVarBytes(ushort.MaxValue);
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(StoreView snapshot)
        {
            return new[] { Sender }; // This address should be checked by consumer
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Witness);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.WriteVarString(Category);
            writer.Write(ValidBlockStart);
            writer.Write(ValidBlockEnd);
            writer.Write(Sender);
            writer.WriteVarBytes(Data);
        }

        public bool Verify(StoreView snapshot)
        {
            uint height = NativeContract.Ledger.CurrentIndex(snapshot);
            if (height < ValidBlockStart || height >= ValidBlockEnd) return false;
            if (!Blockchain.Singleton.IsExtensibleWitnessWhiteListed(Sender)) return false;
            return this.VerifyWitnesses(snapshot, 0_02000000);
        }
    }
}
