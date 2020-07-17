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
        public static readonly byte CurrentVersion = 0;
        public byte Version;
        public uint Index;
        public UInt256 RootHash;
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
            sizeof(byte) +      //Version
            sizeof(uint) +      //Index
            UInt256.Length +    //RootHash
            Witness.Size;       //Witness

        StateRoot ICloneable<StateRoot>.Clone()
        {
            return new StateRoot
            {
                Version = Version,
                Index = Index,
                RootHash = RootHash,
                Witness = Witness,
            };
        }

        void ICloneable<StateRoot>.FromReplica(StateRoot replica)
        {
            Version = replica.Version;
            Index = replica.Index;
            RootHash = replica.RootHash;
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
            RootHash = reader.ReadSerializable<UInt256>();
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
            writer.Write(RootHash);
        }

        public bool Verify(StoreView snapshot)
        {
            return this.VerifyWitnesses(snapshot, 1_00000000);
        }

        public virtual UInt160[] GetScriptHashesForVerifying(StoreView snapshot)
        {
            var script_hash = Blockchain.Singleton.GetBlock(Index)?.NextConsensus;
            if (script_hash is null) throw new System.InvalidOperationException("No script hash for state root verifying");
            return new UInt160[] { script_hash };
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json["version"] = Version;
            json["index"] = Index;
            json["stateroot"] = RootHash.ToString();
            json["witness"] = Witness.ToJson();
            return json;
        }
    }
}
