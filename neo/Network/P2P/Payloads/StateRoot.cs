using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Persistence;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class StateRoot : ISerializable, IInventory
    {
        public byte Version;
        public uint Index;
        public UInt256 PreHash;
        public UInt256 Root;
        public Witness Witness;

        InventoryType IInventory.InventoryType => InventoryType.StateRoot;

        public int Size =>
            sizeof(byte) +          //Version
            sizeof(uint) +          //Index
            PreHash.Size +          //PrevHash
            Root.Size +             //StateRoot
            Witness.Size;           //Witness

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

        Witness[] IVerifiable.Witnesses
        {
            get
            {
                return new[] { Witness };
            }
        }

        public StateRoot() { }

        public void Deserialize(BinaryReader reader)
        {
            ((IVerifiable)this).DeserializeUnsigned(reader);
            Witness[] witnesses = reader.ReadSerializableArray<Witness>(1);
            if (witnesses.Length != 1) return;
            Witness = witnesses[0];
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadByte();
            Index = reader.ReadUInt32();
            PreHash = reader.ReadSerializable<UInt256>();
            Root = reader.ReadSerializable<UInt256>();
        }

        public void Serialize(BinaryWriter writer)
        {
            ((IVerifiable)this).SerializeUnsigned(writer);
            writer.Write(Witness is null ? new Witness[] { } : new Witness[] { Witness });
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Index);
            writer.Write(PreHash);
            writer.Write(Root);
        }

        public bool Verify(Snapshot snapshot)
        {
            if (!this.VerifyWitnesses(snapshot)) return false;
            return true;
        }

        public UInt160[] GetScriptHashesForVerifying(Snapshot snapshot)
        {
            var script_hash = snapshot.GetBlock(Index)?.NextConsensus;
            return script_hash is null ? Array.Empty<UInt160>() : new UInt160[] { script_hash };
        }

        public byte[] GetMessage()
        {
            return this.GetHashData();
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["version"] = Version;
            json["index"] = Index;
            json["prehash"] = PreHash.ToString();
            json["stateroot"] = Root.ToString();
            json["witness"] = Witness is null ? new JObject() : Witness.ToJson();
            return json;
        }
    }
}
