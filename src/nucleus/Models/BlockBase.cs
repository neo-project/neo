using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;

using System;
using System.IO;
using System.Linq;

namespace Neo.Models
{
    public abstract class BlockBase : IWitnessed
    {
        public readonly uint Magic;
        private uint version;
        private UInt256 prevHash;
        private UInt256 merkleRoot;
        private ulong timestamp;
        private uint index;
        private UInt160 nextConsensus;
        private Witness witness;

        public BlockBase(uint magic)
        {
            this.Magic = magic;
        }

        private Lazy<UInt256> hash;
        public UInt256 Hash
        {
            get
            {
                hash ??= new Lazy<UInt256>(() => this.CalculateHash(Magic));
                return hash.Value;
            }
        }

        private Lazy<Witness[]> witnesses;
        Witness[] IWitnessed.Witnesses
        {
            get
            {
                witnesses ??= new Lazy<Witness[]>(() => new Witness[] { witness });
                return witnesses.Value;
            }
        }

        public virtual int Size =>
            sizeof(uint) +       //Version
            UInt256.Length +     //PrevHash
            UInt256.Length +     //MerkleRoot
            sizeof(ulong) +      //Timestamp
            sizeof(uint) +       //Index
            UInt160.Length +     //NextConsensus
            1 +                  //Witness array count
            Witness.Size;        //Witness   

        public uint Version { get => version; set { version = value; hash = null; } }
        public UInt256 PrevHash { get => prevHash; set { prevHash = value; hash = null; } }
        public UInt256 MerkleRoot { get => merkleRoot; set { merkleRoot = value; hash = null; } }
        public ulong Timestamp { get => timestamp; set { timestamp = value; hash = null; } }
        public uint Index { get => index; set { index = value; hash = null; } }
        public UInt160 NextConsensus { get => nextConsensus; set { nextConsensus = value; hash = null; } }
        public Witness Witness { get => witness; set { witness = value; witnesses = null; } }

        public virtual void Deserialize(BinaryReader reader)
        {
            ((IWitnessed)this).DeserializeUnsigned(reader);
            Witness[] witnesses = reader.ReadSerializableArray<Witness>(1);
            if (witnesses.Length != 1) throw new FormatException();
            Witness = witnesses[0];
        }

        void IWitnessed.DeserializeUnsigned(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            MerkleRoot = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt64();
            Index = reader.ReadUInt32();
            NextConsensus = reader.ReadSerializable<UInt160>();
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            ((IWitnessed)this).SerializeUnsigned(writer);
            writer.Write(new Witness[] { Witness });
        }

        void IWitnessed.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Index);
            writer.Write(NextConsensus);
        }

        public virtual JObject ToJson(byte addressVersion)
        {
            JObject json = new JObject();
            json["hash"] = Hash.ToString();
            json["size"] = Size;
            json["version"] = Version;
            json["previousblockhash"] = PrevHash.ToString();
            json["merkleroot"] = MerkleRoot.ToString();
            json["time"] = Timestamp;
            json["index"] = Index;
            json["nextconsensus"] = NextConsensus.ToAddress(addressVersion);
            json["witnesses"] = new JArray(Witness.ToJson());
            return json;
        }

        protected void DeserializeJson(JObject json, byte addressVersion)
        {
            Version = (uint)json["version"].AsNumber();
            PrevHash = UInt256.Parse(json["previousblockhash"].AsString());
            MerkleRoot = UInt256.Parse(json["merkleroot"].AsString());
            Timestamp = (ulong)json["time"].AsNumber();
            Index = (uint)json["index"].AsNumber();
            NextConsensus = json["nextconsensus"].ToScriptHash(addressVersion);
            Witness = ((JArray)json["witnesses"]).Select(p => Witness.FromJson(p)).FirstOrDefault();
        }
    }
}
