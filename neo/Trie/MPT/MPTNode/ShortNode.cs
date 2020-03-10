using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Trie.MPT
{
    public class ShortNode : MPTNode
    {
        public byte[] Key;
        public MPTNode Next;

        protected override byte[] GenHash()
        {
            return Crypto.Default.Hash256(this.Encode());
        }

        public ShortNode()
        {
            nType = NodeType.ShortNode;
        }

        public override void EncodeSpecific(BinaryWriter writer)
        {
            writer.WriteVarBytes(Key);
            var hashNode = new HashNode(Next.GetHash());
            hashNode.EncodeSpecific(writer);
        }

        public override void DecodeSpecific(BinaryReader reader)
        {
            Key = reader.ReadVarBytes();
            var hashNode = new HashNode();
            hashNode.DecodeSpecific(reader);
            Next = hashNode;
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            json["key"] = Key.ToHexString();
            json["next"] = Next.ToJson();
            return json;
        }
    }
}
