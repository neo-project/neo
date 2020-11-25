using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.SmartContract.Native.Designate;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class PluginPayload : IVerifiable
    {
        public string Plugin;
        public byte MessageType;
        public byte[] Data;
        public Role WitnessRole;
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
            Plugin.GetVarSize() +   //Plugin
            sizeof(byte) +          //MessageType
            sizeof(Role) +          //WitnessRole
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
            Plugin = reader.ReadVarString(32);
            MessageType = reader.ReadByte();
            Data = reader.ReadVarBytes(ushort.MaxValue);
            WitnessRole = (Role)reader.ReadByte();
            if (!Enum.IsDefined(typeof(Role), WitnessRole)) throw new FormatException();
        }

        UInt160[] IVerifiable.GetScriptHashesForVerifying(StoreView snapshot)
        {
            return new[] { Blockchain.GetConsensusAddress(NativeContract.Designate.GetDesignatedByRole(snapshot, WitnessRole, snapshot.Height)) };
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write((byte)1); writer.Write(Witness);
        }

        void IVerifiable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.WriteVarString(Plugin);
            writer.Write(MessageType);
            writer.WriteVarBytes(Data);
            writer.Write((byte)WitnessRole);
        }

        public bool Verify(StoreView snapshot)
        {
            return this.VerifyWitnesses(snapshot, 0_02000000);
        }
    }
}
