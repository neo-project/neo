using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class StateRoot : ICloneable<StateRoot>, IInventory
    {
        public byte Version;
        public uint Index;
        public UInt256 Root;
        public Witness Witness;

        InventoryType IInventory.InventoryType => InventoryType.StateRoot;

        private UInt256 _hash = null;

        public UInt256 Hash
        {
            get
            {
                if (_hash == null)
                {
                    _hash = new UInt256(Crypto.Hash256(this.GetHashData()));
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
            set
            {
                if (value.Length != 1) throw new ArgumentException();
                Witness = value[0];
            }
        }

        public int Size =>
            sizeof(byte) +
            sizeof(uint) +
            UInt256.Length +
            Witness.Size;

        StateRoot ICloneable<StateRoot>.Clone()
        {
            return new StateRoot
            {
                Version = Version,
                Index = Index,
                Root = Root,
                Witness = Witness,
            };
        }

        void ICloneable<StateRoot>.FromReplica(StateRoot replica)
        {
            Version = replica.Version;
            Index = replica.Index;
            Root = replica.Root;
            Witness = replica.Witness;
        }

        public void Deserialize(BinaryReader reader)
        {
            this.DeserializeUnsigned(reader);
            Witness = reader.ReadSerializable<Witness>();
        }

        public void DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadByte();
            Index = reader.ReadUInt32();
            Root = reader.ReadSerializable<UInt256>();
        }

        public void Serialize(BinaryWriter writer)
        {
            this.SerializeUnsigned(writer);
            writer.Write(Witness);
        }

        public void SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(Index);
            writer.Write(Root);
        }

        public bool Verify(StoreView snapshot)
        {
            return this.VerifyWitnesses(snapshot,
                ApplicationEngine.ECDsaVerifyPrice * (NativeContract.NEO.GetValidators(Blockchain.Singleton.GetSnapshot()).Length + 1));
        }

        public virtual UInt160[] GetScriptHashesForVerifying(StoreView snapshot)
        {
            var script_hash = Blockchain.Singleton.GetBlock(Index)?.NextConsensus;
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
            json["stateroot"] = Root.ToString();
            json["witness"] = Witness.ToJson();
            return json;
        }
    }
}
