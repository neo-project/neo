using System;
using System.IO;
using System.Linq;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class Header : ISignable
    {
        public uint Version;
        public UInt256 PrevHash;
        public UInt256 MerkleRoot;
        public ulong Timestamp;
        public uint Index;
        public UInt160 NextConsensus;
        public Witness Witness;

        public int Size => 
            sizeof(uint) +       //Version
            UInt256.Length +     //PrevHash
            UInt256.Length +     //MerkleRoot
            sizeof(ulong) +      //Timestamp
            sizeof(uint) +       //Index
            UInt160.Length +     //NextConsensus
            1 +                  //Witness array count
            Witness.Size;        //Witness   

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            MerkleRoot = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt64();
            Index = reader.ReadUInt32();
            NextConsensus = reader.ReadSerializable<UInt160>();
            Witness[] witnesses = reader.ReadSerializableArray<Witness>(1);
            if (witnesses.Length != 1) throw new FormatException();
            Witness = witnesses[0];
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            ((ISignable)this).SerializeUnsigned(writer);
            writer.Write(new Witness[] { Witness });
        }

        Witness[] ISignable.Witnesses => new Witness[] { Witness };

        void ISignable.SerializeUnsigned(BinaryWriter writer)
        {
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(MerkleRoot);
            writer.Write(Timestamp);
            writer.Write(Index);
            writer.Write(NextConsensus);
        }

        public JObject ToJson(uint magic, byte addressVersion)
        {
            JObject json = new JObject();
            json["hash"] = this.CalculateHash(magic).ToString();
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

        public static Header FromJson(JObject json, byte? addressVersion)
        {
            Header header = new Header
            {
                Version = (uint)json["version"].AsNumber(),
                PrevHash = UInt256.Parse(json["previousblockhash"].AsString()),
                MerkleRoot = UInt256.Parse(json["merkleroot"].AsString()),
                Timestamp = (ulong)json["time"].AsNumber(),
                Index = (uint)json["index"].AsNumber(),
                NextConsensus = json["nextconsensus"].ToScriptHash(addressVersion),
                Witness = ((JArray)json["witnesses"]).Select(p => Witness.FromJson(p)).FirstOrDefault()
            };
            return header;
        }
    }
}
