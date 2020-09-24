using System;
using System.IO;
using System.Linq;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class Header : BlockBase, IEquatable<Header>
    {
        public Header(uint magic) : base(magic)
        {
        }

        public override int Size => base.Size + 1;

        public bool Equals(Header other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Hash.Equals(other.Hash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Header);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            if (reader.ReadByte() != 0) throw new FormatException();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write((byte)0);
        }

        // public JObject ToJson(uint magic, byte addressVersion)
        // {
        //     JObject json = new JObject();
        //     json["hash"] = this.CalculateHash(magic).ToString();
        //     json["size"] = Size;
        //     json["version"] = Version;
        //     json["previousblockhash"] = PrevHash.ToString();
        //     json["merkleroot"] = MerkleRoot.ToString();
        //     json["time"] = Timestamp;
        //     json["index"] = Index;
        //     json["nextconsensus"] = NextConsensus.ToAddress(addressVersion);
        //     json["witnesses"] = new JArray(Witness.ToJson());
        //     return json;
        // }

        // public static Header FromJson(JObject json, byte? addressVersion)
        // {
        //     Header header = new Header
        //     {
        //         Version = (uint)json["version"].AsNumber(),
        //         PrevHash = UInt256.Parse(json["previousblockhash"].AsString()),
        //         MerkleRoot = UInt256.Parse(json["merkleroot"].AsString()),
        //         Timestamp = (ulong)json["time"].AsNumber(),
        //         Index = (uint)json["index"].AsNumber(),
        //         NextConsensus = json["nextconsensus"].ToScriptHash(addressVersion),
        //         Witness = ((JArray)json["witnesses"]).Select(p => Witness.FromJson(p)).FirstOrDefault()
        //     };
        //     return header;
        // }
    }
}
