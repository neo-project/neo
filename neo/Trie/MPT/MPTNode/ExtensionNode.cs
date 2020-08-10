using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Trie.MPT
{
    public class ExtensionNode : MPTNode
    {
        //Max StorageKey length
        public const int MaxKeyLength = 1125;
        public byte[] Key;
        public MPTNode Next;

        public ExtensionNode()
        {
            nType = NodeType.ExtensionNode;
        }

        public override void EncodeSpecific(BinaryWriter writer)
        {
            writer.WriteVarBytes(Key);
            var hashNode = new HashNode(Next.GetHash());
            hashNode.EncodeSpecific(writer);
        }

        public override void DecodeSpecific(BinaryReader reader)
        {
            Key = reader.ReadVarBytes(MaxKeyLength);
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
